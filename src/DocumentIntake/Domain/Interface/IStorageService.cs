namespace DocumentIntake.Domain.Interfaces;

public interface IStorageService
{
    Task SaveContentAsync(string id, byte[] content);
    Task<byte[]?> GetContentAsync(string id);
}