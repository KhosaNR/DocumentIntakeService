using DocumentIntake.Domain.Entities;
using DocumentIntake.Domain.Interfaces;
using System.Collections.Concurrent;

namespace DocumentIntake.Infrastructure.Repositories;

public class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<string, Document> _documents = new();
    private readonly ConcurrentDictionary<string, string> _deduplicationIndex = new();

    public Task SaveAsync(Document document, string deduplicationKey)
    {
        _documents[document.Id] = document;
        _deduplicationIndex[deduplicationKey] = document.Id;
        return Task.CompletedTask;
    }

    public Task<Document?> GetByIdAsync(string id)
    {
        _documents.TryGetValue(id, out var doc);
        return Task.FromResult(doc);
    }

    public Task<Document?> GetByDeduplicationKeyAsync(string deduplicationKey)
    {
        if (_deduplicationIndex.TryGetValue(deduplicationKey, out var id))
        {
            return GetByIdAsync(id);
        }
        return Task.FromResult<Document?>(null);
    }

    public Task UpdateStatusAsync(string id, string status)
    {
        if (_documents.TryGetValue(id, out var doc))
        {
            doc.Status = status;
        }
        return Task.CompletedTask;
    }

    public Task AddAuditEntryAsync(string id, string action, string details)
    {
        if (_documents.TryGetValue(id, out var doc))
        {
            doc.AuditTrail.Add(new AuditEntry(action, details, DateTime.UtcNow));
        }
        return Task.CompletedTask;
    }

    public Task SavePreviewAsync(string id, string preview)
    {
        if (_documents.TryGetValue(id, out var doc))
        {
            doc.Preview = preview;
        }
        return Task.CompletedTask;
    }
}