namespace Circlimate.Core;

public interface ITemperatureDataStore
{
    IEnumerable<DailyRecord> GetDailyRecords(string location);
}
