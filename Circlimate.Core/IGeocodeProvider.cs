namespace Circlimate.Core;

public interface IGeocodeProvider
{
    Task<(double Latitiude, double Longitude)> GetLocation(string locationName);
}
