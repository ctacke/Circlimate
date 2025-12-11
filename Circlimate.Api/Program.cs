using Circlimate.Core;
using Circlimate.Data;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Register PostgreSQL DbContext
        builder.Services.AddDbContext<CirclimateDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("CirclimateDb")));

        // Register Circlimate services
        builder.Services.AddSingleton<IGeocodeProvider, MeteoGeocodeProvider>();
        builder.Services.AddSingleton<ITemperatureHistoryProvider, OpenMeteoTemperatureHistoryProvider>();
        builder.Services.AddScoped<ITemperatureDataStore, PostgresTemperatureDataStore>();
        builder.Services.AddScoped<TemperatureDataService>();

        var app = builder.Build();

        // Apply database migrations automatically on startup
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<CirclimateDbContext>();
                context.Database.Migrate();
                var logger = services.GetService<ILogger<Program>>();
                logger?.LogInformation("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                var logger = services.GetService<ILogger<Program>>();
                logger?.LogError(ex, "An error occurred while applying database migrations");
                throw;
            }
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapGet("/temperature/{city}", async (string city, TemperatureDataService service, DateTime? startDate, DateTime? endDate) =>
        {
            try
            {
                var records = await service.GetDailyRecords(city, startDate, endDate);
                var recordsList = records.ToList();

                if (!recordsList.Any())
                {
                    return Results.NotFound(new { message = $"No temperature data found for {city}" });
                }

                return Results.Ok(new
                {
                    city,
                    startDate = startDate ?? new DateTime(1940, 1, 1),
                    endDate = endDate ?? DateTime.UtcNow.AddDays(-7),
                    recordCount = recordsList.Count,
                    records = recordsList.Select(r => new
                    {
                        date = r.Date.ToString("yyyy-MM-dd"),
                        maxTemperatureC = r.MaxTemperature.Celsius,
                        minTemperatureC = r.MinTemperature.Celsius,
                        providerId = r.ProviderId
                    })
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error fetching temperature data",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GetTemperatureData")
        .WithOpenApi()
        .WithDescription("Get historical temperature data for a city. Defaults to all available data from 1940-01-01 if dates not specified.");

        app.Run();
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
