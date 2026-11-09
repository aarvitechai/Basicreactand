using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedStarVibe.Application.Abstractions;
using RedStarVibe.Application.Auth;
using RedStarVibe.Application.Common;
using RedStarVibe.Infrastructure.Email;

namespace RedStarVibe.Infrastructure.Identity;

internal sealed partial class IdentityAuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IDateTimeProvider clock,
    IEmailOutbox emailOutbox,
    IOptions<AppUrlOptions> appUrls,
    IOptions<EmailOptions> emailOptions,
    IOptions<DataProtectionTokenProviderOptions> tokenOptions,
    ILogger<IdentityAuthService> logger) : IAuthService
{
    // Same message for unknown email, wrong password and lockout so responses don't reveal which accounts exist.
    private const string InvalidCredentials = "Invalid email or password.";
    private const string DuplicateEmail = "An account with this email already exists.";
    private const string InvalidResetLink = "This reset link is invalid or has expired. Please request a new one.";

    public async Task<UserDto> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email = command.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = command.DisplayName.Trim(),
            CreatedAtUtc = clock.UtcNow,
        };

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, command.Password);
        }
        catch (DbUpdateException ex)
        {
            // Unique index caught a concurrent registration that slipped past Identity's pre-check.
            throw new ConflictException(DuplicateEmail, ex);
        }

        if (!result.Succeeded)
        {
            ThrowForIdentityErrors(result.Errors);
        }

        LogUserRegistered(logger, user.Id);
        await signInManager.SignInAsync(user, isPersistent: false);
        return ToDto(user);
    }

    public async Task<UserDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByEmailAsync(command.Email.Trim());
        if (user is null)
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        var result = await signInManager.PasswordSignInAsync(user, command.Password, command.RememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            LogUserLockedOut(logger, user.Id);
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        return ToDto(user);
    }

    public Task LogoutAsync() => signInManager.SignOutAsync();

    public async Task<UserDto?> GetUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : ToDto(user);
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            // Deliberately silent: the caller gets the same response whether or not the account exists.
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = appUrls.Value.FrontendBaseUrl.TrimEnd('/');
        var query = $"email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(encodedToken)}";
        var resetLink = new Uri($"{baseUrl}/reset-password?{query}");

        var message = EmailTemplates.PasswordReset(
            user.Email!, user.DisplayName, resetLink, emailOptions.Value.FromName, tokenOptions.Value.TokenLifespan);

        await emailOutbox.EnqueueAsync(message, cancellationToken);
        LogPasswordResetRequested(logger, user.Id);
    }

    public async Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByEmailAsync(command.Email.Trim());
        if (user is null || !TryDecodeToken(command.Token, out var token))
        {
            throw new ValidationException("token", InvalidResetLink);
        }

        var result = await userManager.ResetPasswordAsync(user, token, command.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            {
                throw new ValidationException("token", InvalidResetLink);
            }

            ThrowForIdentityErrors(result.Errors);
        }

        // Proving ownership of the email is enough to lift a lockout. ResetPasswordAsync also rotated the
        // security stamp, so sessions signed in with the old password are invalidated.
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
        LogPasswordReset(logger, user.Id);
    }

    private static bool TryDecodeToken(string encoded, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
            return token.Length > 0;
        }
        catch (FormatException)
        {
            token = string.Empty;
            return false;
        }
    }

    private static UserDto ToDto(ApplicationUser user) =>
        new(user.Id, user.Email!, user.DisplayName, user.CreatedAtUtc);

    private static void ThrowForIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var list = errors.ToList();
        if (list.Exists(e => e.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            throw new ConflictException(DuplicateEmail);
        }

        var grouped = list
            .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "password"
                : e.Code.Contains("Email", StringComparison.Ordinal) || e.Code.Contains("UserName", StringComparison.Ordinal) ? "email"
                : string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

        throw new ValidationException(grouped);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} registered")]
    private static partial void LogUserRegistered(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} locked out after repeated failed logins")]
    private static partial void LogUserLockedOut(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Password reset requested for user {UserId}")]
    private static partial void LogPasswordResetRequested(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Password reset completed for user {UserId}")]
    private static partial void LogPasswordReset(ILogger logger, string userId);
}
