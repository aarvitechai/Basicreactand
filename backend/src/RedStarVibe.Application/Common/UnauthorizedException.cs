namespace RedStarVibe.Application.Common;

/// <summary>Thrown when credentials are missing or invalid. Mapped to HTTP 401.</summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException()
    {
    }

    public UnauthorizedException(string message)
        : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
