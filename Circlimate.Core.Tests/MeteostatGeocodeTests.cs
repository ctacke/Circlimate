namespace Circlimate.Core.Tests;

public class MeteostatGeocodeTests
{
    [Fact]
    // TODO: add traits for multiple locations
    public async Task GetDailyRecords_ShouldSucceed()
    {
        var geocoder = new MeteoGeocodeProvider();
        var location = await geocoder.GetLocation("Paris");
        // Chicago to lat = 41.85003, lon = -87.65005
        // Paris to lat = 48.85341, lon = 2.3488
    }
}
