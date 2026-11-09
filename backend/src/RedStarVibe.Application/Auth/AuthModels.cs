namespace RedStarVibe.Application.Auth;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName, string? PhoneNumber = null);

public sealed record LoginCommand(string Email, string Password, bool RememberMe);

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword);

public sealed record ConfirmEmailCommand(string UserId, string Token);

public sealed record UserDto(
    string Id,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAtUtc,
    bool EmailConfirmed,
    string? PhoneNumber,
    bool PhoneNumberConfirmed);
