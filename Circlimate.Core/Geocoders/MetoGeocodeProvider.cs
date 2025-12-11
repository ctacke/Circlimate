using System.Diagnostics;
using System.Net.Http.Json;

namespace Circlimate.Core;

public class MeteoGeocodeProvider : IGeocodeProvider
{
    private readonly HttpClient _http = new HttpClient();

    public async Task<(double Latitiude, double Longitude)> GetLocation(string city)
    {
        // TODO: add other possible geocoders/providers
        var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}";
        var geo = await _http.GetFromJsonAsync<GeoResponse>(geoUrl);

        if (geo?.Results == null || geo.Results.Length == 0)
        {
            throw new Exception("City not found");
        }

        Debug.WriteLine($"Geocoded {city} to lat={geo.Results[0].Latitude}, lon={geo.Results[0].Longitude}");

        return new(geo.Results[0].Latitude, geo.Results[0].Longitude);
    }

    internal class GeoResponse
    {
        public GeoResult[] Results { get; set; }
    }

    internal class GeoResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
