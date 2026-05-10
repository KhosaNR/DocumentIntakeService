using DocumentIntake.Domain.Interfaces;

namespace DocumentIntake.Application.Workers;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly IMessageQueue _queue;
    private readonly IDocumentRepository _repo;
    private readonly IStorageService _storage;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(
        IMessageQueue queue,
        IDocumentRepository repo,
        IStorageService storage,
        ILogger<DocumentProcessingWorker> logger)
    {
        _queue = queue;
        _repo = repo;
        _storage = storage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Processing Worker starting.");

        await foreach (var message in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing document {InternalId}", message.InternalId);
                await _repo.UpdateStatusAsync(message.InternalId, "Processing");
                await _repo.AddAuditEntryAsync(message.InternalId, "Processing", "Started processing background job.");

                await Task.Delay(1000, stoppingToken);

                var contentBytes = await _storage.GetContentAsync(message.InternalId);
                if (contentBytes != null)
                {
                    var text = System.Text.Encoding.UTF8.GetString(contentBytes);
                    var preview = text.Length > 100 ? text.Substring(0, 100) + "..." : text;

                    await _repo.SavePreviewAsync(message.InternalId, preview);
                }

                await _repo.UpdateStatusAsync(message.InternalId, "Completed");
                await _repo.AddAuditEntryAsync(message.InternalId, "Completed", "Successfully generated preview.");
                _logger.LogInformation("Successfully processed document {InternalId}", message.InternalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {InternalId}", message.InternalId);
                await _repo.UpdateStatusAsync(message.InternalId, "Failed");
                await _repo.AddAuditEntryAsync(message.InternalId, "Failed", $"Processing failed: {ex.Message}");
            }
        }
    }
}