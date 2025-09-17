using Microsoft.EntityFrameworkCore;
using Weatherapp.Models;

namespace Weatherapp.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

        public DbSet<WeatherForecast> WeatherForecasts { get; set; }
        public DbSet<WeatherLocation> WeatherLocations { get; set; }
    }
}
