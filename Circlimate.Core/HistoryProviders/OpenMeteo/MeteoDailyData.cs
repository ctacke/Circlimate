using System.Text.Json.Serialization;

namespace Circlimate.Core;

public class MeteoDailyData
{
    [JsonPropertyName("time")]
    public string[] Time { get; set; }

    [JsonPropertyName("temperature_2m_max")]
    public double?[] TemperatureMax { get; set; }

    [JsonPropertyName("temperature_2m_min")]
    public double?[] TemperatureMin { get; set; }
}
