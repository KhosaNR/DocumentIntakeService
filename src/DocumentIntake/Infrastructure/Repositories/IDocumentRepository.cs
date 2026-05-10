using DocumentIntake.Domain.Entities;

namespace DocumentIntake.Domain.Interfaces;

public interface IDocumentRepository
{
    Task SaveAsync(Document document, string deduplicationKey);
    Task<Document?> GetByIdAsync(string id);
    Task<Document?> GetByDeduplicationKeyAsync(string deduplicationKey);
    Task UpdateStatusAsync(string id, string status);
    Task AddAuditEntryAsync(string id, string action, string details);
    Task SavePreviewAsync(string id, string preview);
}