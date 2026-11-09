namespace RedStarVibe.Application.Auth;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName);

public sealed record LoginCommand(string Email, string Password, bool RememberMe);

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword);

public sealed record UserDto(string Id, string Email, string DisplayName, DateTimeOffset CreatedAtUtc);
