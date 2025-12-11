using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Circlimate.Blazor.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private ILogger<Home> Logger { get; set; } = default!;

    private string city = "Paris";
    private string loadedCity = "";
    private bool isLoading = false;
    private string? errorMessage = null;
    private List<TemperatureRecord> temperatureData = new();
    private int animationKey = 0;
    private bool showMinTemp = true;
    private bool showMaxTemp = true;

    private const double CenterRadius = 50;
    private const double MaxRadius = 300;
    private const double MinTemp = -20;
    private const double MaxTemp = 40;

    private async Task LoadData()
    {
        Logger.LogInformation("LoadData called for city: {City}", city);

        if (string.IsNullOrWhiteSpace(city))
        {
            errorMessage = "Please enter a city name";
            return;
        }

        isLoading = true;
        errorMessage = null;
        temperatureData.Clear();
        StateHasChanged(); // Force UI update to show spinner

        try
        {
            Logger.LogInformation("Fetching data from API for {City}", city);

            // Request 5 years of data
            var endDate = DateTime.UtcNow.AddDays(-7);
            var startDate = endDate.AddYears(-5);

            var response = await Http.GetFromJsonAsync<ApiResponse>(
                $"temperature/{Uri.EscapeDataString(city)}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

            Logger.LogInformation("Response received: {RecordCount} records", response?.RecordCount ?? 0);

            if (response?.Records != null)
            {
                temperatureData = response.Records.ToList();
                loadedCity = response.City ?? city;
                animationKey++; // Trigger animation restart
                Logger.LogInformation("Data loaded successfully: {Count} records", temperatureData.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading data for city {City}", city);
            errorMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged(); // Force UI update
        }
    }

    private double MapTemperatureToRadius(double temperature)
    {
        // Map temperature from [-20, 40] to [CenterRadius, MaxRadius]
        var normalized = (temperature - MinTemp) / (MaxTemp - MinTemp);
        return CenterRadius + (normalized * (MaxRadius - CenterRadius));
    }

    private (double x, double y) GetPointPosition(int dayOfYear, double temperature)
    {
        var angle = (dayOfYear / 365.0 * 360.0 - 90) * Math.PI / 180.0; // -90 to start at top (0 degrees)
        var radius = MapTemperatureToRadius(temperature);

        var x = 400 + Math.Cos(angle) * radius;
        var y = 400 + Math.Sin(angle) * radius;

        return (x, y);
    }

    private string GenerateMaxTempPath()
    {
        if (!temperatureData.Any()) return "";

        var pathData = new System.Text.StringBuilder();

        for (int i = 0; i < temperatureData.Count; i++)
        {
            var record = temperatureData[i];
            var dayOfYear = record.Date.DayOfYear;
            var (x, y) = GetPointPosition(dayOfYear, record.MaxTemperatureC);

            if (i == 0)
                pathData.Append($"M {x:F2} {y:F2}");
            else
                pathData.Append($" L {x:F2} {y:F2}");
        }

        return pathData.ToString();
    }

    private string GenerateMinTempPath()
    {
        if (!temperatureData.Any()) return "";

        var pathData = new System.Text.StringBuilder();

        for (int i = 0; i < temperatureData.Count; i++)
        {
            var record = temperatureData[i];
            var dayOfYear = record.Date.DayOfYear;
            var (x, y) = GetPointPosition(dayOfYear, record.MinTemperatureC);

            if (i == 0)
                pathData.Append($"M {x:F2} {y:F2}");
            else
                pathData.Append($" L {x:F2} {y:F2}");
        }

        return pathData.ToString();
    }

    private string GetDataYear()
    {
        if (!temperatureData.Any()) return "";

        var years = GetYears();
        if (years.Count == 1)
            return years[0].ToString();

        // Show year range
        return $"{years.First()}-{years.Last()}";
    }

    private List<int> GetYears()
    {
        if (!temperatureData.Any()) return new List<int>();

        return temperatureData
            .Select(r => r.Date.Year)
            .Distinct()
            .OrderBy(y => y)
            .ToList();
    }

    private List<TemperatureRecord> GetRecordsForYear(int year)
    {
        return temperatureData
            .Where(r => r.Date.Year == year)
            .OrderBy(r => r.Date.DayOfYear)
            .ToList();
    }

    private string GenerateMaxTempPathForYear(int year)
    {
        var yearData = GetRecordsForYear(year);
        if (!yearData.Any()) return "";

        var pathData = new System.Text.StringBuilder();

        for (int i = 0; i < yearData.Count; i++)
        {
            var record = yearData[i];
            var dayOfYear = record.Date.DayOfYear;
            var (x, y) = GetPointPosition(dayOfYear, record.MaxTemperatureC);

            if (i == 0)
                pathData.Append($"M {x:F2} {y:F2}");
            else
                pathData.Append($" L {x:F2} {y:F2}");
        }

        return pathData.ToString();
    }

    private string GenerateMinTempPathForYear(int year)
    {
        var yearData = GetRecordsForYear(year);
        if (!yearData.Any()) return "";

        var pathData = new System.Text.StringBuilder();

        for (int i = 0; i < yearData.Count; i++)
        {
            var record = yearData[i];
            var dayOfYear = record.Date.DayOfYear;
            var (x, y) = GetPointPosition(dayOfYear, record.MinTemperatureC);

            if (i == 0)
                pathData.Append($"M {x:F2} {y:F2}");
            else
                pathData.Append($" L {x:F2} {y:F2}");
        }

        return pathData.ToString();
    }

    private double GetOpacityForYear(int year, List<int> allYears)
    {
        var index = allYears.IndexOf(year);
        var count = allYears.Count;

        // Oldest year: 0.3, newest year: 1.0
        // Linear interpolation
        return 0.3 + (0.7 * index / Math.Max(1, count - 1));
    }

    private double GetAnimationDelayForYear(int year, List<int> allYears)
    {
        var index = allYears.IndexOf(year);
        // Each year takes 3 seconds, so delay is index * 3
        return index * 3.0;
    }

    private class ApiResponse
    {
        public string? City { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RecordCount { get; set; }
        public TemperatureRecord[]? Records { get; set; }
    }

    private class TemperatureRecord
    {
        public DateTime Date { get; set; }
        public double MaxTemperatureC { get; set; }
        public double MinTemperatureC { get; set; }
        public int ProviderId { get; set; }
    }
}
