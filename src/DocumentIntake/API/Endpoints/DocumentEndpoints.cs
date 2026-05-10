using DocumentIntake.Api.DTOs;
using DocumentIntake.Domain.Interfaces;
using DocumentIntake.Domain.Entities;
using DocumentIntake.Domain.Messages;

namespace DocumentIntake.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Submits a new document for storage and background processing.
        /// </summary>
        /// <param name="provider">The source provider of the document (e.g., Lexis, Westlaw).</param>
        /// <param name="sourceDocumentId">The unique identifier from the upstream provider.</param>
        /// <param name="title">The title of the legal document.</param>
        /// <param name="file">The raw file content to be stored.</param>
        /// <returns>An Accepted status with the newly generated internal Document ID.</returns>
        /// <response code="202">Returns the internal ID when successfully accepted.</response>
        /// <response code="200">Returns the existing ID if a duplicate submission is detected.</response>
        /// <response code="400">If required fields are missing.</response>
        app.MapPost("/api/documents", async (
            HttpContext context,
            IDocumentRepository repo,
            IStorageService storage,
            IMessageQueue queue,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("DocumentEndpoints");
            var form = await context.Request.ReadFormAsync();
            var file = form.Files.GetFile("file");

            var provider = form["provider"].ToString();
            var sourceDocumentId = form["sourceDocumentId"].ToString();
            var title = form["title"].ToString();

            if (file == null || string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(sourceDocumentId))
            {
                logger.LogWarning("Submission failed: Missing required fields.");
                return Results.BadRequest("File, provider, and sourceDocumentId are required.");
            }

            logger.LogInformation("Received document submission: {SourceDocumentId} from {Provider}", sourceDocumentId, provider);

            var deduplicationKey = $"{provider}_{sourceDocumentId}";
            var existingDoc = await repo.GetByDeduplicationKeyAsync(deduplicationKey);

            if (existingDoc != null)
            {
                logger.LogInformation("Duplicate submission detected for {DeduplicationKey}. Ignoring.", deduplicationKey);
                await repo.AddAuditEntryAsync(existingDoc.Id, "Received", "Duplicate submission ignored.");
                return Results.Ok(new SubmissionResponseDto("Document already exists.", existingDoc.Id));
            }

            var internalId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow;

            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            await storage.SaveContentAsync(internalId, fileBytes);

            var metadata = new Document
            {
                Id = internalId,
                SourceDocumentId = sourceDocumentId,
                Provider = provider,
                Title = title,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Status = "Stored",
                ReceivedAt = timestamp
            };

            metadata.AuditTrail.Add(new AuditEntry("Received", "Submission received via API.", timestamp));
            metadata.AuditTrail.Add(new AuditEntry("Stored", "Raw content saved to storage.", DateTime.UtcNow));

            await repo.SaveAsync(metadata, deduplicationKey);

            var message = new ProcessDocumentMessage(internalId, sourceDocumentId, "GeneratePreview");
            await queue.EnqueueAsync(message);

            await repo.UpdateStatusAsync(internalId, "Queued");
            await repo.AddAuditEntryAsync(internalId, "Queued", "Message sent to background worker.");

            logger.LogInformation("Successfully queued document {InternalId} for processing.", internalId);

            return Results.Accepted($"/api/documents/{internalId}", new SubmissionResponseDto("Document accepted.", internalId));
        })
        .DisableAntiforgery()
        .WithName("SubmitDocument")
        .Produces<SubmissionResponseDto>(StatusCodes.Status202Accepted)
        .Produces<SubmissionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        /// <summary>
        /// Retrieves the current status, metadata, and audit trail of a specific document.
        /// </summary>
        /// <param name="id">The internal Document ID generated during submission.</param>
        /// <returns>The document metadata including the generated preview.</returns>
        /// <response code="200">Returns the document details.</response>
        /// <response code="404">If the document cannot be found.</response>
        app.MapGet("/api/documents/{id}", async (string id, IDocumentRepository repo) =>
        {
            var doc = await repo.GetByIdAsync(id);
            if (doc == null) return Results.NotFound();

            var response = new DocumentResponseDto(
                doc.Id,
                doc.SourceDocumentId,
                doc.Provider,
                doc.Title,
                doc.FileName,
                doc.ContentType,
                doc.ReceivedAt,
                doc.Status,
                doc.Preview,
                doc.AuditTrail.Select(a => new AuditEntryDto(a.Action, a.Details, a.Timestamp)).ToList()
            );

            return Results.Ok(response);
        })
        .WithName("GetDocumentStatus")
        .Produces<DocumentResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        /// <summary>
        /// Downloads the raw file content associated with a document.
        /// </summary>
        /// <param name="id">The internal Document ID.</param>
        /// <returns>The raw file bytes.</returns>
        /// <response code="200">Returns the file content.</response>
        /// <response code="404">If the document or the file content cannot be found.</response>
        app.MapGet("/api/documents/{id}/content", async (string id, IStorageService storage, IDocumentRepository repo) =>
        {
            var doc = await repo.GetByIdAsync(id);
            if (doc == null) return Results.NotFound();

            var content = await storage.GetContentAsync(id);
            if (content == null) return Results.NotFound();

            return Results.File(content, doc.ContentType, doc.FileName);
        })
        .WithName("GetDocumentContent")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}