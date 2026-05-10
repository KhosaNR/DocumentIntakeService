namespace DocumentIntake.Domain.Entities;

public class Document
{
    public string Id { get; set; } = string.Empty;
    public string SourceDocumentId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Preview { get; set; }
    public List<AuditEntry> AuditTrail { get; set; } = new();
}

public record AuditEntry(string Action, string Details, DateTime Timestamp);