using Xunit.Abstractions;

namespace Circlimate.Core.Tests;

public class MeteostatHistoryTests
{
    private readonly ITestOutputHelper _output;

    public MeteostatHistoryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    // TODO: add traits for multiple locations
    public async Task GetDailyRecords_ShouldSucceed()
    {
        var geocoder = new MeteoGeocodeProvider();
        var logger = new TestLogger<OpenMeteoTemperatureHistoryProvider>(_output);
        var provider = new OpenMeteoTemperatureHistoryProvider(logger, geocoder);

        // Test with past year, ending 7 days ago to account for API delay
        var endDate = DateTime.UtcNow.AddDays(-7);
        var startDate = endDate.AddYears(-1);

        _output.WriteLine($"Requesting data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

        var records = await provider.GetDailyRecords("Paris", startDate, endDate);
        var recordsArray = records.ToArray();

        _output.WriteLine($"Received {recordsArray.Length} records");

        Assert.NotEmpty(recordsArray);
        Assert.All(recordsArray, r =>
        {
            Assert.True(r.Date >= startDate && r.Date <= endDate);
            Assert.NotNull(r.MaxTemperature);
            Assert.NotNull(r.MinTemperature);
        });
    }
}