using Meadow.Units;

namespace Circlimate.Core;

public record DailyRecord
{
    public DateTime Date { get; init; }
    public Temperature MaxTemperature { get; init; }
    public Temperature MinTemperature { get; init; }
    public int ProviderId { get; init; }
}
