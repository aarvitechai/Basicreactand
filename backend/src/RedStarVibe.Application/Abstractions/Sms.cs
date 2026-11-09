namespace RedStarVibe.Application.Abstractions;

/// <param name="To">Destination in E.164 format, e.g. +919876543210.</param>
public sealed record SmsMessage(string To, string Body);

/// <summary>Delivers a text message (Twilio in production, console log in Development).</summary>
public interface ISmsSender
{
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken);
}
