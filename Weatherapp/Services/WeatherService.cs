using Microsoft.EntityFrameworkCore;
using Weatherapp.Data;
using Weatherapp.Models;

namespace Weatherapp.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly WeatherDbContext _context;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(WeatherDbContext context, ILogger<WeatherService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Ensure we always have a valid LocationId to use for generated forecasts
        private async Task<int> GetOrCreateDefaultLocationIdAsync()
        {
            var loc = await _context.WeatherLocations
                .FirstOrDefaultAsync(l => l.Name == "Default");

            if (loc is null)
            {
                loc = new WeatherLocation { Name = "Default", Country = "N/A" };
                _context.WeatherLocations.Add(loc);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created default location with Id {Id}", loc.Id);
            }

            return loc.Id;
        }

        public async Task<IEnumerable<WeatherForecast>> GetForecastAsync(int days = 5)
        {
            if (days <= 0) return Enumerable.Empty<WeatherForecast>();

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var endDate = startDate.AddDays(days - 1); // inclusive range of N days

            // pull existing forecasts in the window
            var existingForecasts = await _context.WeatherForecasts
                .Where(f => f.Date >= startDate.ToDateTime(TimeOnly.MinValue)
                         && f.Date <= endDate.ToDateTime(TimeOnly.MaxValue))
                .OrderBy(f => f.Date)
                .ToListAsync();

            if (existingForecasts.Count >= days)
                return existingForecasts.Take(days);

            // We'll attach generated forecasts to a valid location
            var defaultLocationId = await GetOrCreateDefaultLocationIdAsync();

            var missing = days - existingForecasts.Count;
            var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

            // figure out which dates are missing
            var haveDates = existingForecasts.Select(f => f.Date.Date).ToHashSet();
            var newForecasts = new List<WeatherForecast>();
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i).ToDateTime(TimeOnly.MinValue);
                if (haveDates.Contains(date.Date)) continue;

                newForecasts.Add(new WeatherForecast
                {
                    Date = date,
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)],
                    Description = $"Generated forecast for {date:yyyy-MM-dd}",
                    CreatedBy = "System",
                    LocationId = defaultLocationId
                });

                if (newForecasts.Count == missing) break;
            }

            if (newForecasts.Count > 0)
            {
                _context.WeatherForecasts.AddRange(newForecasts);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Generated {Count} new weather forecasts", newForecasts.Count);
            }

            return existingForecasts.Concat(newForecasts).OrderBy(f => f.Date);
        }

        public async Task<WeatherForecast> AddForecastAsync(WeatherForecast forecast)
        {
            // Validate FK to avoid FK violation
            var locExists = await _context.WeatherLocations
                .AnyAsync(l => l.Id == forecast.LocationId);

            if (!locExists)
                throw new ArgumentException($"LocationId {forecast.LocationId} does not exist.");

            _context.WeatherForecasts.Add(forecast);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Added new weather forecast for {Date} (LocationId {LocationId})",
                forecast.Date, forecast.LocationId);
            return forecast;
        }

        public async Task<IEnumerable<WeatherLocation>> GetLocationsAsync()
            => await _context.WeatherLocations.ToListAsync();

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
            if (location is null)
                throw new ArgumentException($"Location with ID {locationId} not found");

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var endDate = startDate.AddDays(days - 1);

            var existing = await _context.WeatherForecasts
                .Where(f => f.LocationId == locationId
                         && f.Date >= startDate.ToDateTime(TimeOnly.MinValue)
                         && f.Date <= endDate.ToDateTime(TimeOnly.MaxValue))
                .OrderBy(f => f.Date)
                .ToListAsync();

            if (existing.Count >= days)
                return existing.Take(days);

            var missing = days - existing.Count;
            var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

            var haveDates = existing.Select(f => f.Date.Date).ToHashSet();
            var newOnes = new List<WeatherForecast>();
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i).ToDateTime(TimeOnly.MinValue);
                if (haveDates.Contains(date.Date)) continue;

                newOnes.Add(new WeatherForecast
                {
                    Date = date,
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)],
                    Description = $"Generated forecast for {date:yyyy-MM-dd} ({location.Name})",
                    CreatedBy = "System",
                    LocationId = locationId
                });

                if (newOnes.Count == missing) break;
            }

            if (newOnes.Count > 0)
            {
                _context.WeatherForecasts.AddRange(newOnes);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Generated {Count} new forecasts for LocationId {LocationId}", newOnes.Count, locationId);
            }

            return existing.Concat(newOnes).OrderBy(f => f.Date);
        }
    }
}
