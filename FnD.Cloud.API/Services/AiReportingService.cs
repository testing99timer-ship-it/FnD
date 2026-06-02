using Microsoft.EntityFrameworkCore;
using FnD.Cloud.API.Data;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FnD.Cloud.API.Services;

public class AiReportingService
{
    private readonly CloudDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;

    public AiReportingService(CloudDbContext dbContext, IConfiguration configuration, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _modelId = configuration["Gemini:ModelId"] ?? "gemini-1.5-flash";
    }

    // =========================================================================
    // 1. FIXED: Wires up perfectly to line 52 in your Program.cs
    // =========================================================================
    public async Task<string> GetSalesSummaryAsync()
    {
        if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("PASTE_YOUR_FREE_GEMINI_API_KEY"))
        {
            throw new InvalidOperationException("Gemini API Key is missing or invalid in appsettings.json");
        }

        // Fetch operational numbers from your database
        var totalSales = await _dbContext.Orders.SumAsync(o => o.TotalAmount);
        var totalOrders = await _dbContext.Orders.CountAsync();
        var averageBillValue = totalOrders > 0 ? totalSales / totalOrders : 0;

        string systemPrompt = $@"
        You are 'FnD AI Brain', an expert restaurant business analyst. 
        Analyze the following real-time restaurant performance metrics and write a concise, sharp 3-bullet insight summary for the restaurant owner. 
        Focus strictly on financial health and suggestions to increase the ticket size. Use Indian Rupee (₹) formatting.

        Current Operational Data:
        - Total System Revenue: ₹{totalSales:F2}
        - Total Lifetime Orders: {totalOrders}
        - Average Order Ticket Value: ₹{averageBillValue:F2}
        ";

        return await CallGeminiAsync(systemPrompt);
    }

    // =========================================================================
    // 2. FIXED: Wires up perfectly to line 67 in your Program.cs
    // =========================================================================
    public async Task<string> AskBusinessQuestionAsync(string userQuestion)
    {
        if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("PASTE_YOUR_FREE_GEMINI_API_KEY"))
        {
            throw new InvalidOperationException("Gemini API Key is missing or invalid in appsettings.json");
        }

        // STEP 1: Ask Gemini to generate the T-SQL query
        string sqlGenerationPrompt = $@"
        You are an expert T-SQL developer. Generate a valid, read-only SQL Server query to answer this user question: ""{userQuestion}""
        
        Database Schema:
        - Table: Orders (Columns: Id [int], LocalOrderId [int], OrderDate [datetime2], TotalAmount [decimal(18,2)])

        Rules:
        1. Return ONLY the executable T-SQL query string. 
        2. Do NOT wrap it in markdown block fences like ```sql. Just raw text.
        3. Only use SELECT statements. No modifications allowed.
        ";

        string sqlQuery = await CallGeminiAsync(sqlGenerationPrompt);
        sqlQuery = CleanSqlQuery(sqlQuery);

        // STEP 2: Execute the generated SQL safely against SSMS
        string databaseResultJson = "";
        try
        {
            databaseResultJson = await ExecuteRawSqlToResponseJsonAsync(sqlQuery);
        }
        catch (Exception ex)
        {
            return $"AI attempted to run an invalid query. Engine Error: {ex.Message}\nGenerated Query: {sqlQuery}";
        }

        // STEP 3: Pass data back to Gemini for final interpretation
        string analyticalPrompt = $@"
        You are 'FnD AI Brain', an expert restaurant business analyst. 
        Analyze the raw database results below and provide a friendly, clear, natural language answer to the owner's question.

        Original Question: ""{userQuestion}""
        Executed SQL Query: {sqlQuery}
        Raw Database Output (JSON format): {databaseResultJson}

        Format the output clearly. Use Indian Rupee (₹) formatting for currency values.
        ";

        return await CallGeminiAsync(analyticalPrompt);
    }

    // =========================================================================
    // REUSABLE CORE COMPONENT HELPERS
    // =========================================================================
    private async Task<string> CallGeminiAsync(string prompt)
    {
        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

        var jsonPayload = JsonSerializer.Serialize(requestBody);

        // Clean up any potential copy-paste whitespaces or carriage returns
        string cleanModel = _modelId.Trim();
        string cleanKey = _apiKey.Trim();

        // Use a relative path since the BaseAddress is now configured in Program.cs
        var relativeUrl = $"v1beta/models/{cleanModel}:generateContent?key={cleanKey}";

        var response = await _httpClient.PostAsync(relativeUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

        //var response = await _httpClient.PostAsync(endpointUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini Gateway Error: {response.StatusCode} - {error}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        return doc.RootElement.GetProperty("candidates")[0]
                             .GetProperty("content")
                             .GetProperty("parts")[0]
                             .GetProperty("text")
                             .GetString() ?? "";
    }

    private async Task<string> ExecuteRawSqlToResponseJsonAsync(string query)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = query;
        using var reader = await command.ExecuteReaderAsync();

        var resultsList = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            resultsList.Add(row);
        }

        return JsonSerializer.Serialize(resultsList);
    }

    private string CleanSqlQuery(string query)
    {
        return query.Replace("```sql", "").Replace("```", "").Trim();
    }
}