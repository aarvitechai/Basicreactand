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

    /// <summary>
    /// Waits for an email to <paramref name="to"/> whose subject contains <paramref name="subjectContains"/>
    /// (emails are delivered by a background worker). Returns the most recent match.
    /// </summary>
    public async Task<EmailMessage> WaitForEmailToAsync(string to, string subjectContains = "", int count = 1, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (true)
        {
            var matches = _sent
                .Where(m => string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase)
                         && m.Subject.Contains(subjectContains, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count >= count)
            {
                return matches[^1];
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero || !await _signal.WaitAsync(remaining))
            {
                throw new TimeoutException($"Expected {count} email(s) to {to} with subject containing '{subjectContains}'.");
            }
        }
    }
}
