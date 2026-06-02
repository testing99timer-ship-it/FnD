using System;

namespace FnD.Cloud.API.Models;

public class SyncLog
{
    public int Id { get; set; }
    public DateTime SyncTimestamp { get; set; } = DateTime.UtcNow;
    public string ClientMachineName { get; set; } = "Unknown_Local_POS";
    public int RecordCountProcessed { get; set; }
    public int DuplicateCountSkipped { get; set; }
    public string Status { get; set; } = "Success"; // Success, Warning, Failed
    public string? ErrorDetails { get; set; }
}