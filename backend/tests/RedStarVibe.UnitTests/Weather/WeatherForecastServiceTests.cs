using RedStarVibe.Application.Abstractions;
using RedStarVibe.Application.Common;
using RedStarVibe.Application.Weather;
using RedStarVibe.Domain.Weather;

namespace RedStarVibe.UnitTests.Weather;

public sealed class WeatherForecastServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class RecordingDataSource : IWeatherDataSource
    {
        public DateOnly? RequestedStart { get; private set; }

        public Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(DateOnly startDate, int days, CancellationToken cancellationToken)
        {
            RequestedStart = startDate;
            IReadOnlyList<WeatherForecast> result = Enumerable.Range(0, days)
                .Select(i => new WeatherForecast(startDate.AddDays(i), 20, "Mild"))
                .ToArray();
            return Task.FromResult(result);
        }
    }

    [Fact]
    public async Task GetUpcomingAsync_StartsTomorrow_AndReturnsRequestedCount()
    {
        var source = new RecordingDataSource();
        var service = new WeatherForecastService(source, new FixedClock());

        var result = await service.GetUpcomingAsync(3, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(new DateOnly(2026, 1, 11), source.RequestedStart);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public async Task GetUpcomingAsync_OutOfRange_Throws(int days)
    {
        var service = new WeatherForecastService(new RecordingDataSource(), new FixedClock());

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.GetUpcomingAsync(days, CancellationToken.None));
        Assert.Contains("days", ex.Errors.Keys);
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(100, 212)]
    [InlineData(-40, -40)]
    public void TemperatureF_ConvertsFromCelsius(int celsius, int expectedF)
    {
        Assert.Equal(expectedF, new WeatherForecast(default, celsius, null).TemperatureF);
    }
}
