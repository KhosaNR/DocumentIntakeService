using DocumentIntake.Domain.Interfaces;
using System.Collections.Concurrent;

namespace DocumentIntake.Infrastructure.Storage;

public class InMemoryStorageService : IStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> _storage = new();

    public Task SaveContentAsync(string id, byte[] content)
    {
        _storage[id] = content;
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetContentAsync(string id)
    {
        _storage.TryGetValue(id, out var content);
        return Task.FromResult(content);
    }
}