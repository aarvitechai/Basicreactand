namespace RedStarVibe.Infrastructure.Sms;

/// <summary>Raised by SMS senders. <see cref="InvalidRecipient"/> distinguishes a bad number from a provider outage.</summary>
public sealed class SmsDeliveryException : Exception
{
    public SmsDeliveryException(string message, bool invalidRecipient)
        : base(message)
    {
        InvalidRecipient = invalidRecipient;
    }

    public SmsDeliveryException()
    {
    }

    public SmsDeliveryException(string message)
        : base(message)
    {
    }

    public SmsDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public bool InvalidRecipient { get; }
}
