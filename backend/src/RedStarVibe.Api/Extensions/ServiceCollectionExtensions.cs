using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using RedStarVibe.Api.Middleware;

namespace RedStarVibe.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    public const string CorsPolicy = "Frontend";
    public const string DefaultRateLimitPolicy = "fixed";
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHealthChecks();
        services.AddAuthorization();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddFixedWindowPolicy(options, DefaultRateLimitPolicy, configuration.GetSection("RateLimiting:Default"), 100, 60);
            // Tighter limit on auth endpoints to slow down credential stuffing / signup abuse.
            AddFixedWindowPolicy(options, AuthRateLimitPolicy, configuration.GetSection("RateLimiting:Auth"), 10, 60);
        });

        // Honour X-Forwarded-* headers when running behind a reverse proxy / load balancer.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    private static void AddFixedWindowPolicy(
        Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options,
        string name,
        IConfigurationSection section,
        int defaultPermitLimit,
        int defaultWindowSeconds)
    {
        var permitLimit = section.GetValue("PermitLimit", defaultPermitLimit);
        var window = TimeSpan.FromSeconds(section.GetValue("WindowSeconds", defaultWindowSeconds));

        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window }));
    }
}
