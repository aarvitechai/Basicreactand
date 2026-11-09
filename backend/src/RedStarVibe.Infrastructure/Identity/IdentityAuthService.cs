using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedStarVibe.Application.Abstractions;
using RedStarVibe.Application.Auth;
using RedStarVibe.Application.Common;

namespace RedStarVibe.Infrastructure.Identity;

internal sealed partial class IdentityAuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IDateTimeProvider clock,
    ILogger<IdentityAuthService> logger) : IAuthService
{
    // Same message for unknown email, wrong password and lockout so responses don't reveal which accounts exist.
    private const string InvalidCredentials = "Invalid email or password.";
    private const string DuplicateEmail = "An account with this email already exists.";

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
}
