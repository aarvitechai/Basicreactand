using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RedStarVibe.Api.Contracts;
using RedStarVibe.Api.Extensions;
using RedStarVibe.Application.Auth;

namespace RedStarVibe.Api.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Creates an account and signs the user in (sets the auth cookie).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await authService.RegisterAsync(
            new RegisterUserCommand(request.Email, request.Password, request.DisplayName), cancellationToken);
        return CreatedAtAction(nameof(Me), UserResponse.FromDto(user));
    }

    /// <summary>Signs in with email and password (sets the auth cookie).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await authService.LoginAsync(
            new LoginCommand(request.Email, request.Password, request.RememberMe), cancellationToken);
        return Ok(UserResponse.FromDto(user));
    }

    /// <summary>Signs out (clears the auth cookie).</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync();
        return NoContent();
    }

    /// <summary>Returns the currently signed-in user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await authService.GetUserAsync(userId, cancellationToken);
        return user is null ? Unauthorized() : Ok(UserResponse.FromDto(user));
    }
}
