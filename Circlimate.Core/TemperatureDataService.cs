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

        // TODO: check to see if we have the data in the local store first

        // Query the data from the provider
        var data = await _historyProvider.GetDailyRecords(location, start, end);

        // TODO: store the data in the local store

        _logger?.LogInformation("Retrieved {Count} records for {Location}", data.Count(), location);
        return data;
    }
}
