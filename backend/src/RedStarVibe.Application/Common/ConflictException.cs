namespace RedStarVibe.Application.Common;

/// <summary>Thrown when a request conflicts with existing state (e.g. duplicate email). Mapped to HTTP 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException()
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
