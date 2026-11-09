using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using RedStarVibe.Application.Abstractions;

namespace RedStarVibe.Infrastructure.Sms;

/// <summary>Sends SMS through Twilio's REST API (no SDK dependency). Docs: https://www.twilio.com/docs/sms/api</summary>
internal sealed class TwilioSmsSender(HttpClient httpClient, IOptions<SmsOptions> options) : ISmsSender
{
    // Twilio error codes meaning "this destination number is unusable" (vs. an outage or bad credentials).
    private static readonly HashSet<int> InvalidRecipientCodes = [21211, 21214, 21217, 21408, 21610, 21612, 21614];

    public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var twilio = options.Value.Twilio;

        var form = new Dictionary<string, string> { ["To"] = message.To, ["Body"] = message.Body };
        if (!string.IsNullOrWhiteSpace(twilio.MessagingServiceSid))
        {
            form["MessagingServiceSid"] = twilio.MessagingServiceSid;
        }
        else
        {
            form["From"] = twilio.FromNumber ?? throw new InvalidOperationException("Sms:Twilio:FromNumber is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"2010-04-01/Accounts/{Uri.EscapeDataString(twilio.AccountSid!)}/Messages.json", UriKind.Relative))
        {
            Content = new FormUrlEncodedContent(form),
        };
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{twilio.AccountSid}:{twilio.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        TwilioError? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<TwilioError>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            // Non-JSON error body; fall through with status code only.
        }

        var invalidRecipient = error?.Code is int code && InvalidRecipientCodes.Contains(code);
        throw new SmsDeliveryException(
            $"Twilio rejected the message: HTTP {(int)response.StatusCode}, code {error?.Code}, {error?.Message}",
            invalidRecipient);
    }

    private sealed record TwilioError(int? Code, string? Message);
}
