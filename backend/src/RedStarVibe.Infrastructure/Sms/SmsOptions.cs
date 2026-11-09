namespace RedStarVibe.Infrastructure.Sms;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public TwilioOptions Twilio { get; set; } = new();
}

/// <summary>
/// Twilio credentials. When <see cref="AccountSid"/> is empty, Development logs SMS to the console instead.
/// Supply <see cref="AuthToken"/> as a secret (user-secrets / Sms__Twilio__AuthToken), never in appsettings.
/// </summary>
public sealed class TwilioOptions
{
    public string? AccountSid { get; set; }

    public string? AuthToken { get; set; }

    /// <summary>Sender number in E.164 format. Either this or <see cref="MessagingServiceSid"/> is required.</summary>
    public string? FromNumber { get; set; }

    /// <summary>Optional Messaging Service SID (MG...), preferred over a single number for deliverability.</summary>
    public string? MessagingServiceSid { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccountSid);
}
