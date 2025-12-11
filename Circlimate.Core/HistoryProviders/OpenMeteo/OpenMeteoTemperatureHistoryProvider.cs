using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Circlimate.Core;

public class OpenMeteoTemperatureHistoryProvider : ITemperatureHistoryProvider
{
    private readonly HttpClient _http = new HttpClient();
    private readonly ILogger? _logger;
    private readonly IGeocodeProvider _geocodeProvider;

    public int ID => 1;
    public string Name => "Meteostat";

    public OpenMeteoTemperatureHistoryProvider(ILogger<OpenMeteoTemperatureHistoryProvider>? logger, IGeocodeProvider geocodeProvider)
    {
        _logger = logger;
        _geocodeProvider = geocodeProvider;
    }

    public async Task<IEnumerable<DailyRecord>> GetDailyRecords(string city, DateTime startDate, DateTime endDate)
    {
        var location = await _geocodeProvider.GetLocation(city);

        // TODO: in the future, we may need to query only days after the latest in the local store
        var start = startDate.ToString("yyyy-MM-dd");
        var end = endDate.ToString("yyyy-MM-dd");

        var weatherUrl =
            $"https://archive-api.open-meteo.com/v1/archive?latitude={location.Latitiude}&longitude={location.Longitude}&start_date={start}&end_date={end}&daily=temperature_2m_max,temperature_2m_min&timezone=UTC";

        _logger?.LogInformation("API URL: {Url}", weatherUrl);

        try
        {
            var hist = await _http.GetFromJsonAsync<MeteoHistoryResponse>(weatherUrl);

            _logger?.LogInformation("Response received - Daily is null: {DailyIsNull}", hist?.Daily == null);
            if (hist?.Daily != null)
            {
                _logger?.LogInformation("Time array is null: {TimeIsNull}, length: {TimeLength}", hist.Daily.Time == null, hist.Daily.Time?.Length);
                _logger?.LogInformation("TempMax array is null: {TempMaxIsNull}, length: {TempMaxLength}", hist.Daily.TemperatureMax == null, hist.Daily.TemperatureMax?.Length);
                _logger?.LogInformation("TempMin array is null: {TempMinIsNull}, length: {TempMinLength}", hist.Daily.TemperatureMin == null, hist.Daily.TemperatureMin?.Length);
            }

            if (hist?.Daily == null || hist.Daily.Time == null ||
                hist.Daily.TemperatureMax == null || hist.Daily.TemperatureMin == null)
            {
                _logger?.LogWarning("No temperature data available from Open-Meteo for city {City}", city);
                return Enumerable.Empty<DailyRecord>();
            }

            var allRecords = hist.Daily.Time
                .Select((date, i) => new
                {
                    Date = date,
                    Index = i,
                    MaxTemp = hist.Daily.TemperatureMax[i],
                    MinTemp = hist.Daily.TemperatureMin[i]
                })
                .ToArray();

            var nullCount = allRecords.Count(x => !x.MaxTemp.HasValue || !x.MinTemp.HasValue);
            _logger?.LogInformation("Total records before filtering: {TotalRecords}", allRecords.Length);
            _logger?.LogInformation("Records with null temps: {NullCount}", nullCount);

            var filteredRecords = allRecords
                .Where(x => x.MaxTemp.HasValue && x.MinTemp.HasValue)
                .Select(x => new DailyRecord
                {
                    ProviderId = this.ID,
                    Date = DateTime.Parse(x.Date),
                    MaxTemperature = new Meadow.Units.Temperature(x.MaxTemp.Value, Meadow.Units.Temperature.UnitType.Celsius),
                    MinTemperature = new Meadow.Units.Temperature(x.MinTemp.Value, Meadow.Units.Temperature.UnitType.Celsius)
                })
                .ToArray();

            _logger?.LogInformation("Records after filtering: {FilteredCount}", filteredRecords.Length);

            return filteredRecords;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching weather data from Meteostat for city {City}", city);
            throw new Exception("Error fetching weather data", ex);
        }
    }
}
