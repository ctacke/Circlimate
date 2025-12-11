using Circlimate.Core;

namespace Circlimate.Data;

public class PostgresTemperatureDataStore : ITemperatureDataStore
{
    public IEnumerable<DailyRecord> GetDailyRecords(string location)
    {
        throw new NotImplementedException();
    }
}
