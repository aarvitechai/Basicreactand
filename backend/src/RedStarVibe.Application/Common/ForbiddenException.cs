namespace RedStarVibe.Application.Common;

/// <summary>
/// Thrown when the caller is identified but not allowed to proceed. Mapped to HTTP 403, with
/// <see cref="Code"/> exposed as a machine-readable "code" so clients can react (e.g. offer to resend a link).
/// </summary>
public sealed class ForbiddenException : Exception
{
    public const string EmailNotConfirmed = "email_not_confirmed";

    public ForbiddenException(string message, string code)
        : base(message)
    {
        Code = code;
    }

    public ForbiddenException()
        : this("Forbidden.", "forbidden")
    {
    }

    public ForbiddenException(string message)
        : this(message, "forbidden")
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "forbidden";
    }

    public string Code { get; }
}
