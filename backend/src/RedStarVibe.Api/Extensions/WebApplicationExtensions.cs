namespace RedStarVibe.Api.Extensions;

internal static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
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
        app.UseRateLimiter();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapControllers().RequireRateLimiting(ServiceCollectionExtensions.RateLimitPolicy);

        return app;
    }
}
