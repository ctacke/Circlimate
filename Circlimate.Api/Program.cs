using Circlimate.Core;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Register Circlimate services
        builder.Services.AddSingleton<IGeocodeProvider, MeteoGeocodeProvider>();
        builder.Services.AddSingleton<ITemperatureHistoryProvider, OpenMeteoTemperatureHistoryProvider>();
        builder.Services.AddScoped<TemperatureDataService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        var summaries = new[]
        {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

        app.MapGet("/weatherforecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
            return forecast;
        })
        .WithName("GetWeatherForecast")
        .WithOpenApi();

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
                    startDate = startDate ?? DateTime.UtcNow.AddDays(-7).AddYears(-1),
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
        .WithDescription("Get historical temperature data for a city. Defaults to past year if dates not specified.");

        app.Run();
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
