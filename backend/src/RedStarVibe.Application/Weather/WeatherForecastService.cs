using RedStarVibe.Application.Abstractions;
using RedStarVibe.Application.Common;
using RedStarVibe.Domain.Weather;

namespace RedStarVibe.Application.Weather;

public interface IWeatherForecastService
{
    Task<IReadOnlyList<WeatherForecast>> GetUpcomingAsync(int days, CancellationToken cancellationToken);
}

public sealed class WeatherForecastService(IWeatherDataSource dataSource, IDateTimeProvider clock)
    : IWeatherForecastService
{
    public const int MinDays = 1;
    public const int MaxDays = 14;

    public Task<IReadOnlyList<WeatherForecast>> GetUpcomingAsync(int days, CancellationToken cancellationToken)
    {
        if (days is < MinDays or > MaxDays)
        {
            throw new ValidationException(nameof(days), $"Must be between {MinDays} and {MaxDays}.");
        }

        var tomorrow = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime).AddDays(1);
        return dataSource.GetForecastsAsync(tomorrow, days, cancellationToken);
    }
}
