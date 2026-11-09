using System.Collections.Concurrent;
using RedStarVibe.Application.Abstractions;

namespace RedStarVibe.IntegrationTests;

/// <summary>Test double that records SMS messages instead of sending them.</summary>
public sealed class CapturingSmsSender : ISmsSender
{
    private readonly ConcurrentQueue<SmsMessage> _sent = new();

    public IReadOnlyCollection<SmsMessage> Sent => _sent;

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public SmsMessage LastTo(string phone) => _sent.Last(m => m.To == phone);
}
