using RedStarVibe.Domain.Weather;

namespace RedStarVibe.Api.Contracts;

/// <summary>Public API shape. Kept separate from the domain model so either can evolve independently.</summary>
public sealed record WeatherForecastResponse(DateOnly Date, int TemperatureC, int TemperatureF, string? Summary)
{
    public static WeatherForecastResponse FromDomain(WeatherForecast forecast) =>
        new(forecast.Date, forecast.TemperatureC, forecast.TemperatureF, forecast.Summary);
}
