using Microsoft.AspNetCore.Mvc;
using RedStarVibe.Api.Contracts;
using RedStarVibe.Application.Weather;

namespace RedStarVibe.Api.Controllers.V1;

[ApiController]
[Route("api/v1/weather-forecasts")]
[Produces("application/json")]
public sealed class WeatherForecastsController(IWeatherForecastService service) : ControllerBase
{
    /// <summary>Returns the forecast for the next <paramref name="days"/> days (1–14).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WeatherForecastResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<WeatherForecastResponse>>> Get(
        [FromQuery] int days = 5,
        CancellationToken cancellationToken = default)
    {
        var forecasts = await service.GetUpcomingAsync(days, cancellationToken);
        return Ok(forecasts.Select(WeatherForecastResponse.FromDomain).ToList());
    }
}
