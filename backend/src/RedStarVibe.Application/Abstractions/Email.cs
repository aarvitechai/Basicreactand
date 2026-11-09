namespace RedStarVibe.Application.Abstractions;

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

/// <summary>Delivers an email immediately (SMTP, provider API, ...).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Queues an email for background delivery. Callers return immediately, so response time never depends on
/// whether an email was actually sent (prevents account enumeration via timing).
/// </summary>
public interface IEmailOutbox
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken);
}
