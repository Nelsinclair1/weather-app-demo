namespace Weatherapp.Models
{
    public class WeatherForecast
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }
        public int LocationId { get; set; }
        public WeatherLocation? Location { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    }
}
