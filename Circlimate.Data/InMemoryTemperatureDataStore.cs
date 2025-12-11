using Circlimate.Core;

namespace Circlimate.Data;

public class InMemoryTemperatureDataStore : ITemperatureDataStore
{
    public IEnumerable<DailyRecord> GetDailyRecords(string location)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DailyRecord>> GetDailyRecordsAsync(string location, DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    public Task StoreDailyRecordsAsync(string location, double latitude, double longitude, IEnumerable<DailyRecord> records)
    {
        throw new NotImplementedException();
    }

    public Task<CityMetadata?> GetCityMetadataAsync(string location)
    {
        throw new NotImplementedException();
    }
}
