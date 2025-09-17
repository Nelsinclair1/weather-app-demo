using Microsoft.EntityFrameworkCore;
using Weatherapp.Models;

namespace Weatherapp.Data;

public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options)
    {
    }

    public DbSet<Weatherforecast> Weatherforecasts { get; set; }
    public DbSet<Weatherlocation> Weatherlocations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Weatherforecast
        modelBuilder.Entity<Weatherforecast>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.HasIndex(e => e.Date);
        });

        // Configure Weatherlocation
        modelBuilder.Entity<Weatherlocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.Name, e.Country }).IsUnique();
        });

        // Seed data
        modelBuilder.Entity<Weatherlocation>().HasData(
            new Weatherlocation { Id = 1, Name = "New York", Country = "USA", Latitude = 40.7128, Longitude = -74.0060 },
            new Weatherlocation { Id = 2, Name = "London", Country = "UK", Latitude = 51.5074, Longitude = -0.1278 },
            new Weatherlocation { Id = 3, Name = "Tokyo", Country = "Japan", Latitude = 35.6762, Longitude = 139.6503 },
            new Weatherlocation { Id = 4, Name = "Sydney", Country = "Australia", Latitude = -33.8688, Longitude = 151.2093 }
        );
    }
}