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

        // Default to maximum available historical data from Open-Meteo (1940-01-01)
        var end = endDate ?? DateTime.UtcNow.AddDays(-7);
        var start = startDate ?? new DateTime(1940, 1, 1);

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

        // Fetch from provider in decade chunks to respect API rate limits
        _logger?.LogInformation("Fetching data from provider for {Location} in decade chunks", location);

        var allData = new List<DailyRecord>();
        var currentStart = start;

        while (currentStart < end)
        {
            // Request one decade at a time (or remaining period if less than a decade)
            var currentEnd = currentStart.AddYears(10);
            if (currentEnd > end)
                currentEnd = end;

            _logger?.LogInformation(
                "Fetching decade chunk for {Location}: {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
                location, currentStart, currentEnd);

            try
            {
                var decadeData = await _historyProvider.GetDailyRecords(location, currentStart, currentEnd);
                var decadeList = decadeData.ToList();

                _logger?.LogInformation(
                    "Retrieved {Count} records for decade {Start:yyyy} - {End:yyyy}",
                    decadeList.Count, currentStart.Year, currentEnd.Year);

                allData.AddRange(decadeList);

                // Store decade in cache immediately if data store is available
                if (_dataStore != null && decadeList.Any())
                {
                    try
                    {
                        // Get location coordinates for storage (cache this for efficiency)
                        var geocoded = await _geocodeProvider.GetLocation(location);

                        await _dataStore.StoreDailyRecordsAsync(
                            location,
                            geocoded.Latitiude,
                            geocoded.Longitude,
                            decadeList);

                        _logger?.LogInformation(
                            "Successfully cached {Count} records for decade {Start:yyyy} - {End:yyyy}",
                            decadeList.Count, currentStart.Year, currentEnd.Year);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to cache decade data for {Location}. Continuing.", location);
                    }
                }

                // Add a small delay between decade requests to be respectful of API rate limits
                if (currentEnd < end)
                {
                    _logger?.LogDebug("Waiting 1 second before next decade request...");
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error fetching decade {Start:yyyy} - {End:yyyy} for {Location}",
                    currentStart.Year, currentEnd.Year, location);

                // If we get a rate limit error, stop requesting more data
                if (ex.Message.Contains("429") || ex.Message.Contains("rate limit"))
                {
                    _logger?.LogWarning("Rate limit encountered. Stopping decade requests and returning partial data.");
                    break;
                }

                throw;
            }

            currentStart = currentEnd.AddDays(1);
        }

        _logger?.LogInformation("Retrieved {Count} total records for {Location} from provider", allData.Count, location);
        return allData;
    }
}
