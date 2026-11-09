namespace RedStarVibe.Application.Common;

/// <summary>Thrown when a downstream dependency (e.g. the SMS provider) fails. Mapped to HTTP 503.</summary>
public sealed class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException()
    {
    }

    public ServiceUnavailableException(string message)
        : base(message)
    {
    }

    public ServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
