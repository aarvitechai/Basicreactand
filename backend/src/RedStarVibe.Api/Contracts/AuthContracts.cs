using System.ComponentModel.DataAnnotations;
using RedStarVibe.Application.Auth;

namespace RedStarVibe.Api.Contracts;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    [Required, StringLength(100, MinimumLength = 2)] string DisplayName,
    [MaxLength(32)] string? PhoneNumber = null);

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(128)] string Password,
    bool RememberMe = false);

public sealed record ForgotPasswordRequest(
    [Required, EmailAddress, MaxLength(256)] string Email);

public sealed record ResetPasswordRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(2048)] string Token,
    [Required, StringLength(128, MinimumLength = 8)] string NewPassword);

public sealed record ConfirmEmailRequest(
    [Required, MaxLength(128)] string UserId,
    [Required, MaxLength(2048)] string Token);

public sealed record ResendConfirmationRequest(
    [Required, EmailAddress, MaxLength(256)] string Email);

public sealed record SendPhoneCodeRequest(
    [Required, MaxLength(32)] string PhoneNumber);

public sealed record VerifyPhoneRequest(
    [Required, MaxLength(32)] string PhoneNumber,
    [Required, RegularExpression(@"^\s*\d{6}\s*$", ErrorMessage = "Enter the 6-digit code.")] string Code);

public sealed record MessageResponse(string Message);

public sealed record UserResponse(
    string Id,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAtUtc,
    bool EmailConfirmed,
    string? PhoneNumber,
    bool PhoneNumberConfirmed)
{
    public static UserResponse FromDto(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new(user.Id, user.Email, user.DisplayName, user.CreatedAtUtc,
            user.EmailConfirmed, user.PhoneNumber, user.PhoneNumberConfirmed);
    }
}
