using Microsoft.Extensions.DependencyInjection;
using RedStarVibe.Application.Abstractions;
using RedStarVibe.Application.Weather;
using RedStarVibe.Infrastructure.Time;
using RedStarVibe.Infrastructure.Weather;

namespace RedStarVibe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IWeatherDataSource, RandomWeatherDataSource>();
        return services;
    }
}
