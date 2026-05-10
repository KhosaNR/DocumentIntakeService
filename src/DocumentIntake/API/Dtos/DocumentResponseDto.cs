namespace DocumentIntake.Api.DTOs;

public record DocumentResponseDto(
    string Id,
    string SourceDocumentId,
    string Provider,
    string Title,
    string FileName,
    string ContentType,
    DateTime ReceivedAt,
    string Status,
    string? Preview,
    List<AuditEntryDto> AuditTrail
);

public record AuditEntryDto(string Action, string Details, DateTime Timestamp);