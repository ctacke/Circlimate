namespace Circlimate.Core;

/// <summary>
/// City metadata model containing data coverage and temperature extremes
/// </summary>
public record CityMetadata
{
    public int CityId { get; init; }
    public string CityName { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime? OldestDataDate { get; init; }
    public DateTime? NewestDataDate { get; init; }
    public double? MinTemperatureC { get; init; }
    public double? MaxTemperatureC { get; init; }
    public DateTime LastUpdatedUtc { get; init; }
}
