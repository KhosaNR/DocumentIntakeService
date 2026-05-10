namespace DocumentIntake.Domain.Messages;

public record ProcessDocumentMessage(string InternalId, string SourceDocumentId, string Action);