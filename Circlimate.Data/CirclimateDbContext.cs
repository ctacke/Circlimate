using Circlimate.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Circlimate.Data;

public class CirclimateDbContext : DbContext
{
    public CirclimateDbContext(DbContextOptions<CirclimateDbContext> options)
        : base(options)
    {
    }

    public DbSet<City> Cities { get; set; } = null!;
    public DbSet<TemperatureDataEntity> TemperatureData { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // City entity configuration
        modelBuilder.Entity<City>(entity =>
        {
            // Unique constraint on city_name, latitude, longitude
            entity.HasIndex(c => new { c.CityName, c.Latitude, c.Longitude })
                .IsUnique()
                .HasDatabaseName("uq_city_location");

            // Index on city_name for fast lookups
            entity.HasIndex(c => c.CityName)
                .HasDatabaseName("idx_cities_name");

            // Default value for last_updated_utc
            entity.Property(c => c.LastUpdatedUtc)
                .HasDefaultValueSql("NOW()");
        });

        // TemperatureData entity configuration
        modelBuilder.Entity<TemperatureDataEntity>(entity =>
        {
            // Unique constraint on city_id, record_date, provider_id
            entity.HasIndex(e => new { e.CityId, e.RecordDate, e.ProviderId })
                .IsUnique()
                .HasDatabaseName("uq_city_date_provider");

            // Composite index for date range queries
            entity.HasIndex(e => new { e.CityId, e.RecordDate })
                .HasDatabaseName("idx_temperature_city_date");

            // Index on provider_id for provider-specific queries
            entity.HasIndex(e => e.ProviderId)
                .HasDatabaseName("idx_temperature_provider");

            // Default value for created_utc
            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql("NOW()");

            // Configure foreign key relationship with cascade delete
            entity.HasOne(e => e.City)
                .WithMany(c => c.TemperatureData)
                .HasForeignKey(e => e.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
