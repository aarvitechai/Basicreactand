using Microsoft.Extensions.Logging;
using RedStarVibe.Application.Abstractions;

namespace RedStarVibe.Infrastructure.Email;

/// <summary>
/// DEVELOPMENT ONLY: writes emails (including reset links) to the console log instead of sending them.
/// Never registered outside Development, because the log would then contain live password-reset tokens.
/// </summary>
internal sealed partial class DevelopmentLogEmailSender(ILogger<DevelopmentLogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        LogEmail(logger, message.To, message.Subject, message.TextBody);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "\n===== DEV EMAIL (not sent) =====\nTo: {To}\nSubject: {Subject}\n\n{Body}\n================================")]
    private static partial void LogEmail(ILogger logger, string to, string subject, string body);
}
