using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace Weatherapp.Services;

public interface ITelemetryService
{
    void TrackWeatherRequest(string endpoint, string location = "global");
    void TrackCustomEvent(string eventName, Dictionary<string, string>? properties = null);
    void TrackPerformance(string operationName, TimeSpan duration);
    void TrackDependency(string dependencyType, string target, string operationName, bool success, TimeSpan duration);
}

public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;

    public TelemetryService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void TrackWeatherRequest(string endpoint, string location = "global")
    {
        _telemetryClient.TrackEvent("WeatherRequest", new Dictionary<string, string>
        {
            {"endpoint", endpoint},
            {"location", location},
            {"timestamp", DateTime.UtcNow.ToString("O")}
        });

        // Custom metric
        _telemetryClient.TrackMetric("WeatherRequestCount", 1, new Dictionary<string, string>
        {
            {"endpoint", endpoint},
            {"location", location}
        });
    }

    public void TrackCustomEvent(string eventName, Dictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackEvent(eventName, properties ?? new Dictionary<string, string>());
    }

    public void TrackPerformance(string operationName, TimeSpan duration)
    {
        _telemetryClient.TrackDependency("Performance", operationName, operationName, 
            DateTime.UtcNow.Subtract(duration), duration, true);
        
        _telemetryClient.TrackMetric($"{operationName}_Duration_ms", duration.TotalMilliseconds);
    }

    public void TrackDependency(string dependencyType, string target, string operationName, bool success, TimeSpan duration)
    {
        _telemetryClient.TrackDependency(dependencyType, target, operationName, 
            DateTime.UtcNow.Subtract(duration), duration, success);
    }
}