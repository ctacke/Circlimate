using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Circlimate.Blazor.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private ILogger<Home> Logger { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private string city = "Paris";
    private string loadedCity = "";
    private bool isLoading = false;
    private string? errorMessage = null;
    private List<TemperatureRecord> temperatureData = new();
    private int animationKey = 0;
    private bool showMinTemp = false;
    private bool showMaxTemp = true;
    private int currentAnimatedYear = 0;
    private string visualizationMode = "lines"; // "lines" or "dots"
    private int dotsSampleRate = 7; // Show every 7th day in dots mode (weekly sampling)
    private bool canvasInitialized = false;
    private int animationDelayMs = 2000; // 2 seconds per year for canvas animation
    private CancellationTokenSource? _animationCancellation;
    private bool isRenderingCanvas = false;

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
            Logger.LogInformation("Fetching data from API for {City} with paging", city);

            // Request data in pages for smooth loading and rendering
            var endDate = DateTime.UtcNow.AddDays(-7);
            var startDate = new DateTime(1940, 1, 1);
            var pageSize = 1000; // Fetch 1000 records at a time (about 3 years)

            // Fetch first page
            var firstPage = await FetchPage(city, startDate, endDate, 1, pageSize);

            if (firstPage?.Records != null && firstPage.Records.Any())
            {
                // Add first page of data and start rendering immediately
                temperatureData = firstPage.Records.ToList();
                loadedCity = firstPage.City ?? city;
                animationKey++; // Trigger animation restart

                // Initialize year
                var years = GetYears();
                if (years.Any())
                {
                    currentAnimatedYear = years.First();
                }

                StateHasChanged(); // Show first page immediately

                Logger.LogInformation("First page loaded: {Count} records. Total: {Total}, HasNext: {HasNext}",
                    firstPage.RecordCount, firstPage.TotalRecords, firstPage.HasNextPage);

                // Start year animation for lines mode only
                if (visualizationMode == "lines")
                {
                    _animationCancellation = new CancellationTokenSource();
                    _ = AnimateYearLabelsContinuous();
                }

                // Pre-fetch remaining pages in background while animating
                if (firstPage.HasNextPage)
                {
                    _ = FetchRemainingPagesAsync(city, startDate, endDate, pageSize, firstPage.TotalPages ?? 1);
                }
            }
            else
            {
                errorMessage = "No data found for this city";
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

            // If in dots mode, start canvas rendering
            if (visualizationMode == "dots" && temperatureData.Any())
            {
                _ = RenderDotsToCanvas();
            }
        }
    }

    private async Task<ApiResponse?> FetchPage(string city, DateTime startDate, DateTime endDate, int page, int pageSize)
    {
        try
        {
            var url = $"temperature/{Uri.EscapeDataString(city)}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&page={page}&pageSize={pageSize}";
            var response = await Http.GetFromJsonAsync<ApiResponse>(url);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching page {Page} for {City}", page, city);
            return null;
        }
    }

    private async Task FetchRemainingPagesAsync(string city, DateTime startDate, DateTime endDate, int pageSize, int totalPages)
    {
        for (int page = 2; page <= totalPages; page++)
        {
            try
            {
                Logger.LogInformation("Pre-fetching page {Page} of {Total}", page, totalPages);

                var pageData = await FetchPage(city, startDate, endDate, page, pageSize);

                if (pageData?.Records != null && pageData.Records.Any())
                {
                    // Append new data and update visualization
                    temperatureData.AddRange(pageData.Records);

                    await InvokeAsync(StateHasChanged);

                    Logger.LogInformation("Page {Page} loaded: {Count} records. Total so far: {Total}",
                        page, pageData.RecordCount, temperatureData.Count);
                }

                // Small delay to prevent UI blocking (allows animation to continue smoothly)
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching page {Page}", page);
            }
        }

        Logger.LogInformation("All pages loaded. Total records: {Count}", temperatureData.Count);
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

        // Return the currently animated year
        return currentAnimatedYear > 0 ? currentAnimatedYear.ToString() : GetYears().FirstOrDefault().ToString();
    }

    private async Task AnimateYearLabelsContinuous()
    {
        try
        {
            var token = _animationCancellation?.Token ?? CancellationToken.None;

            // Continuously update year label based on data as it loads
            while (isLoading || temperatureData.Any())
            {
                if (token.IsCancellationRequested)
                    break;

                var years = GetYears();
                if (years.Any())
                {
                    // Cycle through years continuously
                    foreach (var year in years)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        currentAnimatedYear = year;
                        await InvokeAsync(StateHasChanged);

                        // Update year display smoothly (faster updates for continuous feel)
                        await Task.Delay(500, token); // Update every 0.5 seconds

                        // Stop if new data load starts
                        if (!temperatureData.Any())
                            break;
                    }
                }
                else
                {
                    await Task.Delay(100, token);
                }

                // If data is fully loaded and we've shown all years, stop
                if (!isLoading && years.Any())
                {
                    currentAnimatedYear = years.Last();
                    await InvokeAsync(StateHasChanged);
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when cancellation is requested
            Logger.LogDebug("Year animation cancelled");
        }
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

    private List<TemperatureRecord> GetSampledRecordsForYear(int year, int sampleRate)
    {
        // For dots mode: sample data to avoid rendering too many points
        // e.g., sampleRate=7 means show every 7th day (weekly)
        var yearRecords = temperatureData
            .Where(r => r.Date.Year == year)
            .OrderBy(r => r.Date)
            .ToList();

        if (sampleRate <= 1)
            return yearRecords;

        var sampledRecords = new List<TemperatureRecord>();
        for (int i = 0; i < yearRecords.Count; i += sampleRate)
        {
            sampledRecords.Add(yearRecords[i]);
        }

        return sampledRecords;
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
        // No delay between years - continuous animation
        return 0;
    }

    private double GetSequentialAnimationDelay(int yearIndex)
    {
        // Each year takes 0.15 seconds to draw
        // Year 0 starts immediately (delay = 0)
        // Year 1 starts at 0.15s (after year 0 completes)
        // Year 2 starts at 0.30s (after year 1 completes)
        // etc.
        const double animationDurationPerYear = 0.15;
        return yearIndex * animationDurationPerYear;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (visualizationMode == "dots" && temperatureData.Any() && !canvasInitialized)
        {
            await InitializeCanvas();
        }
    }

    private async Task InitializeCanvas()
    {
        try
        {
            canvasInitialized = await JSRuntime.InvokeAsync<bool>(
                "circlimateCanvas.initialize",
                "backgroundCanvas",
                "foregroundCanvas");

            if (canvasInitialized)
            {
                Logger.LogInformation("Canvas initialized successfully");
            }
            else
            {
                Logger.LogError("Canvas initialization returned false");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize canvas");
            canvasInitialized = false;
        }
    }

    private async Task RenderDotsToCanvas()
    {
        // Prevent concurrent rendering
        if (isRenderingCanvas)
        {
            Logger.LogWarning("Canvas rendering already in progress, skipping duplicate call");
            return;
        }

        try
        {
            isRenderingCanvas = true;

            if (!canvasInitialized)
            {
                await InitializeCanvas();
                if (!canvasInitialized)
                {
                    Logger.LogError("Cannot render: Canvas not initialized");
                    return;
                }
            }

            var years = GetYears();
            if (!years.Any())
            {
                Logger.LogWarning("No years to render");
                return;
            }

            Logger.LogInformation("Starting canvas rendering for {YearCount} years with {DelayMs}ms per year",
                years.Count, animationDelayMs);

            // Clear both canvases
            try
            {
                await JSRuntime.InvokeVoidAsync("circlimateCanvas.clearAll");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error clearing canvases");
                return;
            }

            foreach (var year in years)
            {
                try
                {
                    // Update year label FIRST so it's visible during drawing
                    currentAnimatedYear = year;
                    await InvokeAsync(StateHasChanged);

                    Logger.LogDebug("Drawing year {Year}", year);

                    // Fade background and transfer foreground to background
                    var fadeAmount = 0.85; // Retain 85% opacity (15% fade per year)
                    await JSRuntime.InvokeVoidAsync("circlimateCanvas.fadeBackgroundAndTransfer", fadeAmount);

                    // Get ALL records for this year (no sampling needed with Canvas!)
                    var yearRecords = GetRecordsForYear(year);
                    var dotsData = new List<object>();

                    Logger.LogDebug("Year {Year} has {Count} records", year, yearRecords.Count);

                    foreach (var record in yearRecords)
                    {
                        var dayOfYear = record.Date.DayOfYear;

                        if (showMaxTemp)
                        {
                            var maxPos = GetPointPosition(dayOfYear, record.MaxTemperatureC);
                            dotsData.Add(new {
                                x = maxPos.x,
                                y = maxPos.y,
                                color = "rgba(200, 0, 0, 1.0)", // Darker red for leading edge visibility
                                radius = 2.5 // Slightly larger for current year
                            });
                        }

                        if (showMinTemp)
                        {
                            var minPos = GetPointPosition(dayOfYear, record.MinTemperatureC);
                            dotsData.Add(new {
                                x = minPos.x,
                                y = minPos.y,
                                color = "rgba(0, 0, 200, 1.0)", // Darker blue for leading edge visibility
                                radius = 2.5 // Slightly larger for current year
                            });
                        }
                    }

                    // Draw all dots for this year
                    await JSRuntime.InvokeVoidAsync("circlimateCanvas.drawDots", dotsData);

                    // Delay before next year (configurable)
                    await Task.Delay(animationDelayMs);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error rendering year {Year}", year);
                    // Continue with next year instead of stopping completely
                }
            }

            Logger.LogInformation("Canvas rendering complete");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fatal error in canvas rendering");
        }
        finally
        {
            isRenderingCanvas = false;
        }
    }

    private async Task OnVisualizationModeChanged(string newMode)
    {
        // Cancel any ongoing background animation
        _animationCancellation?.Cancel();
        _animationCancellation?.Dispose();
        _animationCancellation = null;

        visualizationMode = newMode;
        canvasInitialized = false; // Reset canvas state when switching modes
        await InvokeAsync(StateHasChanged);

        if (newMode == "dots" && temperatureData.Any())
        {
            Logger.LogInformation("Switching to Canvas dots mode");
            // Give DOM time to create canvas elements
            await Task.Delay(100);
            _ = RenderDotsToCanvas(); // Fire and forget
        }
    }

    private async Task OnTempSeriesChanged(bool showMax)
    {
        showMaxTemp = showMax;
        showMinTemp = !showMax;

        // Trigger animation restart for lines mode
        if (visualizationMode == "lines")
        {
            animationKey++;
        }

        // Re-render for canvas mode
        if (visualizationMode == "dots" && temperatureData.Any())
        {
            _ = RenderDotsToCanvas();
        }

        await InvokeAsync(StateHasChanged);
    }

    private class ApiResponse
    {
        public string? City { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RecordCount { get; set; }
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int? TotalPages { get; set; }
        public bool HasNextPage { get; set; }
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
