using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RedStarVibe.Application.Common;

namespace RedStarVibe.Api.Middleware;

/// <summary>Converts unhandled exceptions into RFC 7807 problem-details responses.</summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = validation.Message,
            },
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = conflict.Message,
            },
            UnauthorizedException unauthorized => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = unauthorized.Message,
            },
            ForbiddenException forbidden => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = forbidden.Message,
                Extensions = { ["code"] = forbidden.Code },
            },
            ServiceUnavailableException unavailable => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = unavailable.Message,
            },
            OperationCanceledException => new ProblemDetails
            {
                Status = StatusCodes.Status499ClientClosedRequest,
                Title = "Request was cancelled.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            },
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}
