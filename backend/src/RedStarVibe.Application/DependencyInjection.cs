using Microsoft.Extensions.DependencyInjection;
using RedStarVibe.Application.Weather;

namespace RedStarVibe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWeatherForecastService, WeatherForecastService>();
        return services;
    }
}
