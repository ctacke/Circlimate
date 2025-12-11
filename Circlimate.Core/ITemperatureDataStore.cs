namespace Circlimate.Core;

public interface ITemperatureDataStore
{
    /// <summary>
    /// Gets all daily records for a location (legacy method)
    /// </summary>
    IEnumerable<DailyRecord> GetDailyRecords(string location);

    /// <summary>
    /// Gets daily records for a location within a date range
    /// </summary>
    Task<IEnumerable<DailyRecord>> GetDailyRecordsAsync(
        string location,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Stores daily records for a location
    /// </summary>
    Task StoreDailyRecordsAsync(
        string location,
        double latitude,
        double longitude,
        IEnumerable<DailyRecord> records);

    /// <summary>
    /// Gets city metadata including data coverage and temperature extremes
    /// </summary>
    Task<CityMetadata?> GetCityMetadataAsync(string location);
}
