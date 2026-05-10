using DocumentIntake.Application.Workers;
using DocumentIntake.Domain.Messages;
using DocumentIntake.Infrastructure.Messaging;
using DocumentIntake.Infrastructure.Repositories;
using DocumentIntake.Infrastructure.Storage;
using DocumentIntake.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DocumentIntake.Tests.Workers;

[TestClass]
public class DocumentProcessingTests
{
    [TestMethod]
    public async Task Worker_ProcessesDocument_AndGeneratesPreview()
    {
        // Arrange
        var queue = new InMemoryMessageQueue();
        var repo = new InMemoryDocumentRepository();
        var storage = new InMemoryStorageService();
        var logger = NullLogger<DocumentProcessingWorker>.Instance;
        var worker = new DocumentProcessingWorker(queue, repo, storage, logger);

        var internalId = Guid.NewGuid().ToString();
        var fileContent = "These are contents of a long legal document that requires processing to generate a summary. It has sufficient length to test the logic.";

        var metadata = new Document { Id = internalId, Status = "Queued" };
        await repo.SaveAsync(metadata, "Provider_123");
        await storage.SaveContentAsync(internalId, Encoding.UTF8.GetBytes(fileContent));

        var message = new ProcessDocumentMessage(internalId, "123", "GeneratePreview");
        await queue.EnqueueAsync(message);

        // Act
        var cts = new CancellationTokenSource();
        var executeTask = worker.StartAsync(cts.Token);

        await Task.Delay(1500);
        cts.Cancel();

        // Assert
        var doc = await repo.GetByIdAsync(internalId);
        Assert.IsNotNull(doc);
        Assert.AreEqual("Completed", doc.Status);
        Assert.IsNotNull(doc.Preview);
        Assert.StartsWith("These are contents of a long legal document", doc.Preview);

        var hasCompletedAudit = doc.AuditTrail.Any(a => a.Action == "Completed");
        Assert.IsTrue(hasCompletedAudit);
    }

    [TestMethod]
    public async Task Deduplication_Prevents_Overwriting_Existing_Document()
    {
        // Arrange
        var repo = new InMemoryDocumentRepository();
        var doc1 = new Document { Id = "ID-1", SourceDocumentId = "SRC-1", Provider = "Lexis" };
        var deduplicationKey = "Lexis_SRC-1";

        // Act
        await repo.SaveAsync(doc1, deduplicationKey);

        var duplicateCheck = await repo.GetByDeduplicationKeyAsync(deduplicationKey);

        // Assert
        Assert.IsNotNull(duplicateCheck);
        Assert.AreEqual("ID-1", duplicateCheck.Id);
    }
}