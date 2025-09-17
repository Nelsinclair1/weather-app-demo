using Weatherapp.Models;

namespace Weatherapp.Services
{
    public interface IWeatherService
    {
        Task<IEnumerable<WeatherForecast>> GetForecastAsync(int days = 5);
        Task<WeatherForecast> AddForecastAsync(WeatherForecast forecast);
        Task<IEnumerable<WeatherLocation>> GetLocationsAsync();
        Task<WeatherLocation> AddLocationAsync(WeatherLocation location);
        Task<IEnumerable<WeatherForecast>> GetForecastByLocationAsync(int locationId, int days = 5);
    }
}
