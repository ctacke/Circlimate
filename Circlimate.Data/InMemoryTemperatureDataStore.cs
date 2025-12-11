using Circlimate.Core;

namespace Circlimate.Data;

public class InMemoryTemperatureDataStore : ITemperatureDataStore
{
    public IEnumerable<DailyRecord> GetDailyRecords(string location)
    {
        throw new NotImplementedException();
    }
}
