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
using RedStarVibe.Infrastructure.Sms;

namespace RedStarVibe.Infrastructure.Identity;

internal sealed partial class IdentityAuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IDateTimeProvider clock,
    IEmailOutbox emailOutbox,
    ISmsSender smsSender,
    IOptions<AppUrlOptions> appUrls,
    IOptions<EmailOptions> emailOptions,
    IOptions<DataProtectionTokenProviderOptions> resetTokenOptions,
    IOptions<EmailConfirmationTokenProviderOptions> confirmTokenOptions,
    ILogger<IdentityAuthService> logger) : IAuthService
{
    // Same message for unknown email, wrong password and lockout so responses don't reveal which accounts exist.
    private const string InvalidCredentials = "Invalid email or password.";
    private const string DuplicateEmail = "An account with this email already exists. Sign in, or resend the confirmation email.";
    private const string InvalidResetLink = "This reset link is invalid or has expired. Please request a new one.";
    private const string InvalidConfirmLink = "This confirmation link is invalid or has expired. Please request a new one.";
    private const string EmailNotConfirmed = "Please confirm your email address before signing in. Check your inbox for the link.";
    private const string InvalidSmsCode = "That code is incorrect or has expired. Request a new code and try again.";

    private string AppName => emailOptions.Value.FromName;

    public async Task<UserDto> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            phone = PhoneNumbers.Normalize(command.PhoneNumber)
                ?? throw new ValidationException("phoneNumber", PhoneNumbers.InvalidMessage);
        }

        var email = command.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = command.DisplayName.Trim(),
            CreatedAtUtc = clock.UtcNow,
            PhoneNumber = phone, // Saved unverified; confirmed later via SMS code.
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
        await SendConfirmationEmailAsync(user, cancellationToken);
        return ToDto(user);
    }

    public async Task<UserDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByEmailAsync(command.Email.Trim());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        // SignInManager rejects unconfirmed accounts *before* checking the password, which would let anyone
        // discover unconfirmed emails. So for unconfirmed users we verify the password first (still counting
        // failures toward lockout) and only reveal "not confirmed" to someone who knows the password.
        if (!user.EmailConfirmed)
        {
            if (!await userManager.CheckPasswordAsync(user, command.Password))
            {
                await userManager.AccessFailedAsync(user);
                throw new UnauthorizedException(InvalidCredentials);
            }

            await userManager.ResetAccessFailedCountAsync(user);
            throw new ForbiddenException(EmailNotConfirmed, ForbiddenException.EmailNotConfirmed);
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

    public async Task ConfirmEmailAsync(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null || !TryDecodeToken(command.Token, out var token))
        {
            throw new ValidationException("token", InvalidConfirmLink);
        }

        if (user.EmailConfirmed)
        {
            return; // Idempotent: clicking the link twice is fine.
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            throw new ValidationException("token", InvalidConfirmLink);
        }

        LogEmailConfirmed(logger, user.Id);
    }

    public async Task ResendEmailConfirmationAsync(string email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || user.EmailConfirmed)
        {
            return; // Deliberately silent (see interface docs).
        }

        await SendConfirmationEmailAsync(user, cancellationToken);
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
        var resetLink = BuildFrontendLink("reset-password", ("email", user.Email!), ("token", EncodeToken(token)));

        var message = EmailTemplates.PasswordReset(
            user.Email!, user.DisplayName, resetLink, AppName, resetTokenOptions.Value.TokenLifespan);

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

        // Using a link from the inbox proves ownership of the email, so it also confirms the address and
        // lifts any lockout. ResetPasswordAsync rotated the security stamp, invalidating old sessions.
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
        LogPasswordReset(logger, user.Id);
    }

    public async Task SendPhoneVerificationCodeAsync(string userId, string phoneNumber, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedException("Your session has expired. Please sign in again.");

        var phone = PhoneNumbers.Normalize(phoneNumber)
            ?? throw new ValidationException("phoneNumber", PhoneNumbers.InvalidMessage);

        var code = await userManager.GenerateChangePhoneNumberTokenAsync(user, phone);
        var body = $"{code} is your {AppName} verification code. It's valid for a few minutes. Never share this code with anyone.";

        try
        {
            await smsSender.SendAsync(new SmsMessage(phone, body), cancellationToken);
        }
        catch (SmsDeliveryException ex) when (ex.InvalidRecipient)
        {
            throw new ValidationException("phoneNumber", "We couldn't send a text to this number. Please check it and try again.");
        }
        catch (Exception ex) when ((ex is SmsDeliveryException or HttpRequestException or TaskCanceledException)
                                   && !cancellationToken.IsCancellationRequested)
        {
            LogSmsFailed(logger, ex, user.Id);
            throw new ServiceUnavailableException("We couldn't send the text message right now. Please try again in a moment.", ex);
        }

        LogPhoneCodeSent(logger, user.Id);
    }

    public async Task<UserDto> VerifyPhoneAsync(string userId, string phoneNumber, string code, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedException("Your session has expired. Please sign in again.");

        var phone = PhoneNumbers.Normalize(phoneNumber)
            ?? throw new ValidationException("phoneNumber", PhoneNumbers.InvalidMessage);

        var result = await userManager.ChangePhoneNumberAsync(user, phone, code.Trim());
        if (!result.Succeeded)
        {
            throw new ValidationException("code", InvalidSmsCode);
        }

        // ChangePhoneNumberAsync rotates the security stamp; re-issue this session's cookie so the user
        // isn't signed out at the next stamp validation.
        await signInManager.RefreshSignInAsync(user);
        LogPhoneVerified(logger, user.Id);
        return ToDto(user);
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = BuildFrontendLink("confirm-email", ("userId", user.Id), ("token", EncodeToken(token)));
        var message = EmailTemplates.ConfirmEmail(
            user.Email!, user.DisplayName, link, AppName, confirmTokenOptions.Value.TokenLifespan);
        await emailOutbox.EnqueueAsync(message, cancellationToken);
    }

    private Uri BuildFrontendLink(string path, params (string Key, string Value)[] query)
    {
        var baseUrl = appUrls.Value.FrontendBaseUrl.TrimEnd('/');
        var qs = string.Join('&', query.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"));
        return new Uri($"{baseUrl}/{path}?{qs}");
    }

    private static string EncodeToken(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

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
        new(user.Id, user.Email!, user.DisplayName, user.CreatedAtUtc,
            user.EmailConfirmed, user.PhoneNumber, user.PhoneNumberConfirmed);

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

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} confirmed their email")]
    private static partial void LogEmailConfirmed(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} locked out after repeated failed logins")]
    private static partial void LogUserLockedOut(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Password reset requested for user {UserId}")]
    private static partial void LogPasswordResetRequested(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Password reset completed for user {UserId}")]
    private static partial void LogPasswordReset(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Phone verification code sent for user {UserId}")]
    private static partial void LogPhoneCodeSent(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} verified their phone number")]
    private static partial void LogPhoneVerified(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending SMS verification code failed for user {UserId}")]
    private static partial void LogSmsFailed(ILogger logger, Exception exception, string userId);
}
