using Microsoft.Extensions.Logging;
using RedStarVibe.Application.Abstractions;

namespace RedStarVibe.Infrastructure.Sms;

/// <summary>
/// DEVELOPMENT ONLY: writes SMS messages (including verification codes) to the console log instead of sending.
/// Never registered outside Development.
/// </summary>
internal sealed partial class DevelopmentLogSmsSender(ILogger<DevelopmentLogSmsSender> logger) : ISmsSender
{
    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        LogSms(logger, message.To, message.Body);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "\n===== DEV SMS (not sent) =====\nTo: {To}\n{Body}\n==============================")]
    private static partial void LogSms(ILogger logger, string to, string body);
}
