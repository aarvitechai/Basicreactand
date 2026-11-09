using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RedStarVibe.Api.Contracts;
using RedStarVibe.Api.Extensions;
using RedStarVibe.Application.Auth;

namespace RedStarVibe.Api.Controllers.V1;

/// <summary>Phone number verification by SMS code for the signed-in user.</summary>
[ApiController]
[Route("api/v1/account/phone")]
[Produces("application/json")]
[Authorize]
public sealed class AccountPhoneController(IAuthService authService) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user has no id claim.");

    /// <summary>Texts a 6-digit verification code to the given number.</summary>
    /// <remarks>Rate-limited per user (SMS costs money and is a target for toll fraud).</remarks>
    [HttpPost("send-code")]
    [EnableRateLimiting(ServiceCollectionExtensions.SmsRateLimitPolicy)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MessageResponse>> SendCode(SendPhoneCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await authService.SendPhoneVerificationCodeAsync(UserId, request.PhoneNumber, cancellationToken);
        return Accepted(new MessageResponse("We've texted you a 6-digit code."));
    }

    /// <summary>Checks the SMS code and saves the number as verified.</summary>
    [HttpPost("verify")]
    [EnableRateLimiting(ServiceCollectionExtensions.SmsVerifyRateLimitPolicy)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<UserResponse>> Verify(VerifyPhoneRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await authService.VerifyPhoneAsync(UserId, request.PhoneNumber, request.Code, cancellationToken);
        return Ok(UserResponse.FromDto(user));
    }
}
