using Circlimate.Core;
using Circlimate.Data.Entities;
using Meadow.Units;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Circlimate.Data;

public class PostgresTemperatureDataStore : ITemperatureDataStore
{
    private readonly CirclimateDbContext _context;
    private readonly ILogger<PostgresTemperatureDataStore>? _logger;

    public PostgresTemperatureDataStore(
        CirclimateDbContext context,
        ILogger<PostgresTemperatureDataStore>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    /// <summary>
    /// Gets all daily records for a location (legacy synchronous method)
    /// </summary>
    public IEnumerable<DailyRecord> GetDailyRecords(string location)
    {
        return GetDailyRecordsAsync(location, DateTime.MinValue, DateTime.MaxValue)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Gets daily records for a location within a date range
    /// </summary>
    public async Task<IEnumerable<DailyRecord>> GetDailyRecordsAsync(
        string location,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            _logger?.LogDebug(
                "Retrieving cached records for {Location} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                location, startDate, endDate);

            var records = await _context.TemperatureData
                .AsNoTracking()
                .Include(td => td.City)
                .Where(td => td.City.CityName == location
                          && td.RecordDate >= startDate.Date
                          && td.RecordDate <= endDate.Date)
                .OrderBy(td => td.RecordDate)
                .Select(td => new DailyRecord
                {
                    Date = td.RecordDate,
                    MaxTemperature = new Temperature(td.MaxTemperatureC, Temperature.UnitType.Celsius),
                    MinTemperature = new Temperature(td.MinTemperatureC, Temperature.UnitType.Celsius),
                    ProviderId = td.ProviderId
                })
                .ToListAsync();

            _logger?.LogInformation(
                "Retrieved {Count} cached records for {Location} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                records.Count, location, startDate, endDate);

            return records;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving temperature data from cache for {Location}", location);
            return Enumerable.Empty<DailyRecord>();
        }
    }

    /// <summary>
    /// Stores daily records for a location
    /// </summary>
    public async Task StoreDailyRecordsAsync(
        string location,
        double latitude,
        double longitude,
        IEnumerable<DailyRecord> records)
    {
        var recordsList = records.ToList();

        if (!recordsList.Any())
        {
            _logger?.LogDebug("No records to store for {Location}", location);
            return;
        }

        try
        {
            _logger?.LogDebug(
                "Storing {Count} records for {Location} (lat={Lat}, lon={Lon})",
                recordsList.Count, location, latitude, longitude);

            // Start transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Find or create city
                var city = await _context.Cities
                    .FirstOrDefaultAsync(c => c.CityName == location
                                           && c.Latitude == latitude
                                           && c.Longitude == longitude);

                if (city == null)
                {
                    city = new City
                    {
                        CityName = location,
                        Latitude = latitude,
                        Longitude = longitude,
                        LastUpdatedUtc = DateTime.UtcNow
                    };
                    _context.Cities.Add(city);
                    await _context.SaveChangesAsync();

                    _logger?.LogInformation("Created new city: {Location} (ID={CityId})", location, city.CityId);
                }
                else
                {
                    city.LastUpdatedUtc = DateTime.UtcNow;
                }

                // Insert temperature records (EF Core will ignore duplicates due to unique constraint)
                var newRecords = new List<TemperatureDataEntity>();

                foreach (var record in recordsList)
                {
                    // Check if record already exists
                    var exists = await _context.TemperatureData
                        .AnyAsync(td => td.CityId == city.CityId
                                     && td.RecordDate == record.Date.Date
                                     && td.ProviderId == record.ProviderId);

                    if (!exists)
                    {
                        newRecords.Add(new TemperatureDataEntity
                        {
                            CityId = city.CityId,
                            RecordDate = record.Date.Date,
                            MaxTemperatureC = record.MaxTemperature.Celsius,
                            MinTemperatureC = record.MinTemperature.Celsius,
                            ProviderId = record.ProviderId,
                            CreatedUtc = DateTime.UtcNow
                        });
                    }
                }

                if (newRecords.Any())
                {
                    _context.TemperatureData.AddRange(newRecords);
                    await _context.SaveChangesAsync();

                    _logger?.LogInformation(
                        "Inserted {NewCount} new records for {Location} (total attempted: {Total})",
                        newRecords.Count, location, recordsList.Count);
                }
                else
                {
                    _logger?.LogInformation(
                        "All {Count} records already exist for {Location}",
                        recordsList.Count, location);
                }

                // Update city metadata
                await UpdateCityMetadataAsync(city.CityId);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error storing temperature data for {Location}", location);
            throw;
        }
    }

    /// <summary>
    /// Gets city metadata
    /// </summary>
    public async Task<CityMetadata?> GetCityMetadataAsync(string location)
    {
        try
        {
            var city = await _context.Cities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CityName == location);

            if (city == null)
            {
                return null;
            }

            return new CityMetadata
            {
                CityId = city.CityId,
                CityName = city.CityName,
                Latitude = city.Latitude,
                Longitude = city.Longitude,
                OldestDataDate = city.OldestDataDate,
                NewestDataDate = city.NewestDataDate,
                MinTemperatureC = city.MinTemperatureC,
                MaxTemperatureC = city.MaxTemperatureC,
                LastUpdatedUtc = city.LastUpdatedUtc
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving city metadata for {Location}", location);
            return null;
        }
    }

    /// <summary>
    /// Updates city metadata based on temperature records
    /// </summary>
    private async Task UpdateCityMetadataAsync(int cityId)
    {
        var city = await _context.Cities.FindAsync(cityId);

        if (city == null)
        {
            return;
        }

        var stats = await _context.TemperatureData
            .Where(td => td.CityId == cityId)
            .GroupBy(td => td.CityId)
            .Select(g => new
            {
                OldestDate = g.Min(td => td.RecordDate),
                NewestDate = g.Max(td => td.RecordDate),
                MinTemp = g.Min(td => td.MinTemperatureC),
                MaxTemp = g.Max(td => td.MaxTemperatureC)
            })
            .FirstOrDefaultAsync();

        if (stats != null)
        {
            city.OldestDataDate = stats.OldestDate;
            city.NewestDataDate = stats.NewestDate;
            city.MinTemperatureC = stats.MinTemp;
            city.MaxTemperatureC = stats.MaxTemp;
            city.LastUpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger?.LogDebug(
                "Updated metadata for city {CityId}: Date range {OldestDate:yyyy-MM-dd} to {NewestDate:yyyy-MM-dd}, Temp range {MinTemp}°C to {MaxTemp}°C",
                cityId, stats.OldestDate, stats.NewestDate, stats.MinTemp, stats.MaxTemp);
        }
    }
}
