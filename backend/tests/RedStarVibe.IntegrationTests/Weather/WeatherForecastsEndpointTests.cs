using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RedStarVibe.Api.Contracts;

namespace RedStarVibe.IntegrationTests.Weather;

public sealed class WeatherForecastsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_ReturnsRequestedNumberOfForecasts()
    {
        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastResponse>>(
            new Uri("/api/v1/weather-forecasts?days=7", UriKind.Relative));

        Assert.NotNull(forecasts);
        Assert.Equal(7, forecasts.Count);
    }

    [Fact]
    public async Task Get_InvalidDays_ReturnsProblemDetails400()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/weather-forecasts?days=99", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
