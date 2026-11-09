namespace RedStarVibe.Application.Abstractions;

/// <summary>Abstracts the system clock so time-dependent logic is testable.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
