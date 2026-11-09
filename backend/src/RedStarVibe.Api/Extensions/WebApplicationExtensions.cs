using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RedStarVibe.Infrastructure.Persistence;

namespace RedStarVibe.Api.Extensions;

internal static class WebApplicationExtensions
{
    public static async Task<WebApplication> UseApiPipelineAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            await app.Services.MigrateDatabaseAsync();
        }

        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors(ServiceCollectionExtensions.CorsPolicy);
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        // Liveness: is the process up? Readiness: can it serve traffic (database reachable)?
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
        app.MapHealthChecks("/health");

        app.MapControllers();

        return app;
    }
}
