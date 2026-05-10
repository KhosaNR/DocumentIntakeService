using DocumentIntake.Domain.Messages;
using DocumentIntake.Domain.Interfaces;
using System.Threading.Channels;

namespace DocumentIntake.Infrastructure.Messaging;

public class InMemoryMessageQueue : IMessageQueue
{
    private readonly Channel<ProcessDocumentMessage> _channel = Channel.CreateUnbounded<ProcessDocumentMessage>();

    public async Task EnqueueAsync(ProcessDocumentMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    public IAsyncEnumerable<ProcessDocumentMessage> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}