using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Circlimate.Data.Entities;

[Table("cities")]
public class City
{
    [Key]
    [Column("city_id")]
    public int CityId { get; set; }

    [Required]
    [Column("city_name")]
    [MaxLength(255)]
    public string CityName { get; set; } = string.Empty;

    [Required]
    [Column("latitude")]
    public double Latitude { get; set; }

    [Required]
    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("oldest_data_date")]
    public DateTime? OldestDataDate { get; set; }

    [Column("newest_data_date")]
    public DateTime? NewestDataDate { get; set; }

    [Column("min_temperature_c")]
    public double? MinTemperatureC { get; set; }

    [Column("max_temperature_c")]
    public double? MaxTemperatureC { get; set; }

    [Required]
    [Column("last_updated_utc")]
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<TemperatureDataEntity> TemperatureData { get; set; } = new List<TemperatureDataEntity>();
}
