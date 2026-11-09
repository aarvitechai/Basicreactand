namespace RedStarVibe.Application.Auth;

/// <summary>Account use cases. Implemented in Infrastructure on top of ASP.NET Core Identity.</summary>
public interface IAuthService
{
    /// <summary>Creates the account and signs the new user in.</summary>
    /// <exception cref="Common.ConflictException">An account with this email already exists.</exception>
    /// <exception cref="Common.ValidationException">The password does not meet the policy.</exception>
    Task<UserDto> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken);

    /// <exception cref="Common.UnauthorizedException">Invalid credentials or the account is locked out.</exception>
    Task<UserDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken);

    Task LogoutAsync();

    Task<UserDto?> GetUserAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Emails a password-reset link if an account exists for <paramref name="email"/>.
    /// Behaves identically whether or not the account exists, so callers can't probe for registered emails.
    /// </summary>
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);

    /// <summary>Sets a new password using a token from the reset email, and unlocks the account.</summary>
    /// <exception cref="Common.ValidationException">The token is invalid/expired, or the password does not meet the policy.</exception>
    Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken);
}
