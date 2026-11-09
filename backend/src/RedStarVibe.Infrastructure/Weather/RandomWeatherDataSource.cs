using RedStarVibe.Application.Weather;
using RedStarVibe.Domain.Weather;

namespace RedStarVibe.Infrastructure.Weather;

/// <summary>
/// Sample data source that generates random forecasts.
/// Replace with a real implementation (HTTP client, EF Core repository, ...) when available.
/// </summary>
internal sealed class RandomWeatherDataSource : IWeatherDataSource
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(DateOnly startDate, int days, CancellationToken cancellationToken)
    {
        IReadOnlyList<WeatherForecast> forecasts = Enumerable.Range(0, days)
            .Select(offset => new WeatherForecast(
                startDate.AddDays(offset),
#pragma warning disable CA5394 // Not security-sensitive: sample data only.
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]))
#pragma warning restore CA5394
            .ToArray();

        return Task.FromResult(forecasts);
    }
}
