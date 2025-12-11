using System.Text.Json.Serialization;

namespace Circlimate.Core;

public class MeteoHistoryResponse
{
    [JsonPropertyName("daily")]
    public MeteoDailyData Daily { get; set; }
}
