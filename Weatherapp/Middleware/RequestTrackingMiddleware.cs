using Weatherapp.Services;
using System.Diagnostics;
// Add the correct using directive if ITelemetryService is in a different namespace
// using YourNamespaceForTelemetryService;

namespace Weatherapp.Middleware;

public class RequestTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<RequestTrackingMiddleware> _logger;

    public RequestTrackingMiddleware(RequestDelegate next, ITelemetryService telemetryService, ILogger<RequestTrackingMiddleware> logger)
    {
        _next = next;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "";

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Track API requests
            if (path.StartsWith("/api/"))
            {
                _telemetryService.TrackCustomEvent("APIRequest", new Dictionary<string, string>
                {
                    {"path", path},
                    {"method", context.Request.Method},
                    {"statusCode", context.Response.StatusCode.ToString()},
                    {"duration", stopwatch.ElapsedMilliseconds.ToString()}
                });

                _telemetryService.TrackPerformance($"API_{context.Request.Method}_{path.Replace("/", "_")}", stopwatch.Elapsed);
            }

            // Log slow requests
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("Slow request detected: {Path} took {Duration}ms", path, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}