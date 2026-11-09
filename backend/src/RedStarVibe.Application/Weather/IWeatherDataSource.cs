using RedStarVibe.Domain.Weather;

namespace RedStarVibe.Application.Weather;

/// <summary>Port for wherever forecast data comes from (external API, database, generator...).</summary>
public interface IWeatherDataSource
{
    Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(DateOnly startDate, int days, CancellationToken cancellationToken);
}
