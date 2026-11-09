using System.Threading.Channels;
using RedStarVibe.Application.Abstractions;

namespace RedStarVibe.Infrastructure.Email;

/// <summary>
/// In-process queue drained by <see cref="EmailDispatcher"/>. Messages not yet sent are lost if the process
/// stops; move to a durable queue (database outbox, Azure Service Bus, ...) if that matters for your use case.
/// </summary>
internal sealed class EmailOutbox : IEmailOutbox
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(capacity: 1000) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(message, cancellationToken);
}
