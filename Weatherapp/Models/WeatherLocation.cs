namespace Weatherapp.Models
{
    public class WeatherLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Country { get; set; }
        public ICollection<WeatherForecast>? Forecasts { get; set; }
    }
}
