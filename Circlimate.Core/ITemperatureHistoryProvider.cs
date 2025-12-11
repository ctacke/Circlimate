namespace Circlimate.Core;

public interface ITemperatureHistoryProvider
{
    int ID { get; }
    string Name { get; }
    Task<IEnumerable<DailyRecord>> GetDailyRecords(string location, DateTime startDate, DateTime endDate);
}
