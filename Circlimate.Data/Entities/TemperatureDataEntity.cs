using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Circlimate.Data.Entities;

[Table("temperature_data")]
public class TemperatureDataEntity
{
    [Key]
    [Column("temperature_data_id")]
    public long TemperatureDataId { get; set; }

    [Required]
    [Column("city_id")]
    public int CityId { get; set; }

    [Required]
    [Column("record_date")]
    public DateTime RecordDate { get; set; }

    [Required]
    [Column("max_temperature_c")]
    public double MaxTemperatureC { get; set; }

    [Required]
    [Column("min_temperature_c")]
    public double MinTemperatureC { get; set; }

    [Required]
    [Column("provider_id")]
    public int ProviderId { get; set; }

    [Required]
    [Column("created_utc")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("CityId")]
    public City City { get; set; } = null!;
}
