namespace RedStarVibe.Domain.Weather;

/// <summary>A single day's weather forecast.</summary>
public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)Math.Round(TemperatureC * 9.0 / 5.0);
}
