using System.ComponentModel.DataAnnotations;

namespace Weatherapp.Models;

public class WeatherForecast
{
    public int Id { get; set; }
    
    [Required]
    public DateOnly Date { get; set; }
    
    [Range(-100, 100)]
    public int TemperatureC { get; set; }
    
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    
    [Required]
    [MaxLength(50)]
    public string Summary { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string? CreatedBy { get; set; }
}

public class WeatherLocation
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;
    
    [Range(-90, 90)]
    public double Latitude { get; set; }
    
    [Range(-180, 180)]
    public double Longitude { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<WeatherForecast> Forecasts { get; set; } = new List<WeatherForecast>();
}