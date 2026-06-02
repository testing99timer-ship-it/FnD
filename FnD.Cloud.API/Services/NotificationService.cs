using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FnD.Cloud.API.Services;

public class NotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationService> _logger;
    private readonly string _webhookUrl;

    public NotificationService(HttpClient httpClient, IConfiguration configuration, ILogger<NotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        // Grab an optional webhook URL from appsettings.json
        _webhookUrl = configuration["Notifications:SlackWebhookUrl"] ?? string.Empty;
    }

    /// <summary>
    /// Dispatches an immediate instant-message alert to a corporate chat channel (Slack/Teams).
    /// </summary>
    public async Task SendInstantWebhookAlertAsync(string title, string message, string severity = "Warning")
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            _logger.LogWarning("Notification Gateway: Webhook URL is unconfigured. Alert skipped: {Title}", title);
            return;
        }

        var emoji = severity.ToLower() == "critical" ? "🚨" : "⚠️";

        // Construct a structured payload compatible with standard Slack/Discord incoming hooks
        var payload = new
        {
            text = $"{emoji} *[{severity.ToUpper()}] {title}*\n{message}\n_Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC_"
        };

        try
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_webhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Outbound webhook alert sent successfully: {Title}", title);
            }
            else
            {
                _logger.LogError("Webhook gateway returned error code: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch out-of-band webhook notification.");
        }
    }

    /// <summary>
    /// Simulates a standard transactional email dispatcher for automated business updates.
    /// </summary>
    public Task SendEmailSummaryAsync(string recipientEmail, string subject, string htmlBody)
    {
        // Mocking SMTP implementation - ready to wire up MailKit or SendGrid
        _logger.LogInformation("Email dispatched cleanly to [{Email}] with Subject: '{Subject}'", recipientEmail, subject);
        return Task.CompletedTask;
    }
}