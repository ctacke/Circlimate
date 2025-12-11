using Microsoft.Extensions.Logging;

namespace Circlimate.Core;

public class TemperatureDataService
{
    private readonly ILogger<TemperatureDataService>? _logger;
    private readonly IGeocodeProvider _geocodeProvider;
    private readonly ITemperatureHistoryProvider _historyProvider;
    private readonly ITemperatureDataStore? _dataStore;

    public TemperatureDataService(
        IGeocodeProvider geocodeProvider,
        ITemperatureHistoryProvider historyProvider,
        ITemperatureDataStore? dataStore = null,
        ILogger<TemperatureDataService>? logger = null)
    {
        _logger = logger;
        _geocodeProvider = geocodeProvider;
        _historyProvider = historyProvider;
        _dataStore = dataStore;
    }

    public async Task<IEnumerable<DailyRecord>> GetDailyRecords(string location, DateTime? startDate = null, DateTime? endDate = null)
    {
        _logger?.LogInformation("Fetching temperature data for location: {Location}", location);

        // Default to past year if dates not specified
        var end = endDate ?? DateTime.UtcNow.AddDays(-7);
        var start = startDate ?? end.AddYears(-1);

        // Check cache first if data store is available
        if (_dataStore != null)
        {
            var cachedRecords = await _dataStore.GetDailyRecordsAsync(location, start, end);
            var cachedList = cachedRecords.ToList();

            // Calculate expected number of days in the date range
            var expectedDays = (end - start).Days + 1;

            // Check if we have sufficient coverage (95% threshold)
            var coveragePercentage = expectedDays > 0 ? (cachedList.Count * 100.0 / expectedDays) : 0;
            var hasSufficientCoverage = coveragePercentage >= 95.0;

            if (hasSufficientCoverage)
            {
                _logger?.LogInformation(
                    "Cache hit: Returning {Count} cached records for {Location} ({Coverage:F1}% coverage)",
                    cachedList.Count, location, coveragePercentage);
                return cachedList;
            }

            _logger?.LogInformation(
                "Insufficient cached data for {Location}: {Count}/{Expected} records ({Coverage:F1}% coverage). Fetching from provider.",
                location, cachedList.Count, expectedDays, coveragePercentage);
        }

        // Fetch from provider
        _logger?.LogInformation("Fetching data from provider for {Location}", location);
        var data = await _historyProvider.GetDailyRecords(location, start, end);
        var dataList = data.ToList();

        // Store in cache if data store is available
        if (_dataStore != null && dataList.Any())
        {
            try
            {
                // Get location coordinates for storage
                var geocoded = await _geocodeProvider.GetLocation(location);

                _logger?.LogInformation(
                    "Storing {Count} records in cache for {Location} (lat={Lat:F5}, lon={Lon:F5})",
                    dataList.Count, location, geocoded.Latitiude, geocoded.Longitude);

                await _dataStore.StoreDailyRecordsAsync(
                    location,
                    geocoded.Latitiude,
                    geocoded.Longitude,
                    dataList);

                _logger?.LogInformation("Successfully cached {Count} records for {Location}", dataList.Count, location);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to cache data for {Location}. Continuing with provider data.", location);
                // Don't fail the request if caching fails - just continue with provider data
            }
        }

        _logger?.LogInformation("Retrieved {Count} records for {Location} from provider", dataList.Count, location);
        return dataList;
    }
}
