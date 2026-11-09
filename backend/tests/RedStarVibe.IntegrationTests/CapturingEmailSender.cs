using System.Collections.Concurrent;
using RedStarVibe.Application.Abstractions;

namespace RedStarVibe.IntegrationTests;

/// <summary>Test double that records emails instead of sending them.</summary>
public sealed class CapturingEmailSender : IEmailSender, IDisposable
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();
    private readonly SemaphoreSlim _signal = new(0);

    public IReadOnlyCollection<EmailMessage> Sent => _sent;

    public void Dispose() => _signal.Dispose();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        _signal.Release();
        return Task.CompletedTask;
    }

    /// <summary>Waits (emails are delivered by a background worker) for an email to the given address.</summary>
    public async Task<EmailMessage> WaitForEmailToAsync(string to, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (true)
        {
            var match = _sent.FirstOrDefault(m => string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero || !await _signal.WaitAsync(remaining))
            {
                throw new TimeoutException($"No email to {to} was sent.");
            }
        }
    }
}
