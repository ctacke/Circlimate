namespace Circlimate.Core;

public class NoaaTemperatureHistoryProvider : ITemperatureHistoryProvider
{
    public int ID => 1;
    public string Name => "Meteostat";

    public Task<IEnumerable<DailyRecord>> GetDailyRecords(string location, DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }
}