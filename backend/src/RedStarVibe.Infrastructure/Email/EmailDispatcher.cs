using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedStarVibe.Application.Abstractions;

namespace RedStarVibe.Infrastructure.Email;

/// <summary>Background worker that sends queued emails, retrying transient failures with backoff.</summary>
internal sealed partial class EmailDispatcher(
    EmailOutbox queue,
    IEmailSender sender,
    ILogger<EmailDispatcher> logger) : BackgroundService
{
    private const int MaxAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            await SendWithRetryAsync(message, stoppingToken);
        }
    }

    private async Task SendWithRetryAsync(EmailMessage message, CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await sender.SendAsync(message, stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // A failed email must never crash the worker; it is logged instead.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                if (attempt == MaxAttempts)
                {
                    LogSendFailed(logger, ex, message.Subject, attempt);
                    return;
                }

                LogSendRetry(logger, ex, message.Subject, attempt);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), stoppingToken);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sending email '{Subject}' failed (attempt {Attempt}); retrying")]
    private static partial void LogSendRetry(ILogger logger, Exception exception, string subject, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Giving up sending email '{Subject}' after {Attempt} attempts")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, string subject, int attempt);
}
