namespace RedStarVibe.Application.Auth;

/// <summary>Account use cases. Implemented in Infrastructure on top of ASP.NET Core Identity.</summary>
public interface IAuthService
{
    /// <summary>
    /// Creates the account and emails a confirmation link. The user is NOT signed in: sign-in is refused
    /// until the email is confirmed.
    /// </summary>
    /// <exception cref="Common.ConflictException">An account with this email already exists.</exception>
    /// <exception cref="Common.ValidationException">The password or phone number is invalid.</exception>
    Task<UserDto> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken);

    /// <exception cref="Common.UnauthorizedException">Invalid credentials or the account is locked out.</exception>
    /// <exception cref="Common.ForbiddenException">Correct password, but the email isn't confirmed yet.</exception>
    Task<UserDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken);

    Task LogoutAsync();

    Task<UserDto?> GetUserAsync(string userId, CancellationToken cancellationToken);

    /// <exception cref="Common.ValidationException">The confirmation link is invalid or expired.</exception>
    Task ConfirmEmailAsync(ConfirmEmailCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Re-sends the confirmation link if an unconfirmed account exists for <paramref name="email"/>.
    /// Behaves identically otherwise, so callers can't probe for registered emails.
    /// </summary>
    Task ResendEmailConfirmationAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Emails a password-reset link if an account exists for <paramref name="email"/>.
    /// Behaves identically whether or not the account exists, so callers can't probe for registered emails.
    /// </summary>
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);

    /// <summary>Sets a new password using a token from the reset email, confirms the email and unlocks the account.</summary>
    /// <exception cref="Common.ValidationException">The token is invalid/expired, or the password does not meet the policy.</exception>
    Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken);

    /// <summary>Texts a 6-digit verification code to <paramref name="phoneNumber"/> for the signed-in user.</summary>
    /// <exception cref="Common.ValidationException">The phone number is not a valid international number.</exception>
    Task SendPhoneVerificationCodeAsync(string userId, string phoneNumber, CancellationToken cancellationToken);

    /// <summary>Verifies the SMS code and saves the number as the user's confirmed phone.</summary>
    /// <exception cref="Common.ValidationException">The code is wrong or expired.</exception>
    Task<UserDto> VerifyPhoneAsync(string userId, string phoneNumber, string code, CancellationToken cancellationToken);
}
