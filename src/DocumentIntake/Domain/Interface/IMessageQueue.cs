using DocumentIntake.Domain.Messages;

namespace DocumentIntake.Domain.Interfaces;

public interface IMessageQueue
{
    Task EnqueueAsync(ProcessDocumentMessage message);
    IAsyncEnumerable<ProcessDocumentMessage> DequeueAsync(CancellationToken cancellationToken);
}