using Microsoft.EntityFrameworkCore;
using Weatherapp.Data;
using Weatherapp.Models;

namespace Weatherapp.Services;

public interface IWeatherService
{
    Task<IEnumerable<Weatherforecast>> GetForecastAsync(int days = 5);
    Task<Weatherforecast> AddForecastAsync(Weatherforecast forecast);
    Task<IEnumerable<Weatherlocation>> GetLocationsAsync();
    Task<Weatherlocation> AddLocationAsync(Weatherlocation location);
    Task<IEnumerable<Weatherforecast>> GetForecastByLocationAsync(int locationId, int days = 5);
}

public class WeatherService : IWeatherService
{
    private readonly WeatherDbContext _context;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(WeatherDbContext context, ILogger<WeatherService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<WeatherForecast>> GetForecastAsync(int days = 5)
    {
        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var endDate = startDate.AddDays(days);

        var existingForecasts = await _context.WeatherForecasts
            .Where(f => f.Date >= startDate && f.Date <= endDate)
            .OrderBy(f => f.Date)
            .ToListAsync();

        if (existingForecasts.Count >= days)
        {
            return existingForecasts.Take(days);
        }

        // Generate missing forecasts
        var missingDays = days - existingForecasts.Count;
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

        var newForecasts = new List<WeatherForecast>();
        for (int i = 0; i < missingDays; i++)
        {
            var date = startDate.AddDays(existingForecasts.Count + i);
            var forecast = new WeatherForecast
            {
                Date = date,
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = summaries[Random.Shared.Next(summaries.Length)],
                Description = $"Generated forecast for {date:yyyy-MM-dd}",
                CreatedBy = "System"
            };
            newForecasts.Add(forecast);
        }

        if (newForecasts.Any())
        {
            _context.WeatherForecasts.AddRange(newForecasts);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Generated {Count} new weather forecasts", newForecasts.Count);
        }

        return existingForecasts.Concat(newForecasts).OrderBy(f => f.Date);
    }

    public async Task<WeatherForecast> AddForecastAsync(WeatherForecast forecast)
    {
        _context.WeatherForecasts.Add(forecast);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added new weather forecast for {Date}", forecast.Date);
        return forecast;
    }

    public async Task<IEnumerable<WeatherLocation>> GetLocationsAsync()
    {
        return await _context.WeatherLocations.ToListAsync();
    }

    public async Task<WeatherLocation> AddLocationAsync(WeatherLocation location)
    {
        _context.WeatherLocations.Add(location);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added new weather location: {Name}, {Country}", location.Name, location.Country);
        return location;
    }

    public async Task<IEnumerable<WeatherForecast>> GetForecastByLocationAsync(int locationId, int days = 5)
    {
        var location = await _context.WeatherLocations.FindAsync(locationId);
        if (location == null)
        {
            throw new ArgumentException($"Location with ID {locationId} not found");
        }

        // For demo purposes, return similar to global forecast
        // In real implementation, you'd have location-specific data
        return await GetForecastAsync(days);
    }
}
