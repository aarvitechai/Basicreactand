using System.ComponentModel.DataAnnotations;
using RedStarVibe.Application.Auth;

namespace RedStarVibe.Api.Contracts;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    [Required, StringLength(100, MinimumLength = 2)] string DisplayName);

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(128)] string Password,
    bool RememberMe = false);

public sealed record UserResponse(string Id, string Email, string DisplayName, DateTimeOffset CreatedAtUtc)
{
    public static UserResponse FromDto(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new(user.Id, user.Email, user.DisplayName, user.CreatedAtUtc);
    }
}
