using System.Security.Claims;
using System.Text.Json.Serialization; // 👈 added
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

using Weatherapp.Authorization;
using Weatherapp.Data;
using Weatherapp.Models;
using Weatherapp.Services;

var builder = WebApplication.CreateBuilder(args);

// 👇 Option B: ignore JSON reference cycles globally
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseSqlServer(connectionString));

// Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

// AuthN (Azure AD) + AuthZ (policies)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RequireWeatherReadAccess, policy =>
        policy.Requirements.Add(new WeatherReadRequirement()));

    options.AddPolicy(Policies.RequireWeatherWriteAccess, policy =>
        policy.Requirements.Add(new WeatherWriteRequirement()));

    options.AddPolicy(Policies.RequireAdminAccess, policy =>
        policy.Requirements.Add(new AdminRequirement()));
});

// Your custom handlers/services
builder.Services.AddScoped<IAuthorizationHandler, WeatherAuthorizationHandler>();
builder.Services.AddScoped<Weatherapp.Services.IAuthenticationService, Weatherapp.Services.AuthenticationService>();

// Weather service
builder.Services.AddScoped<IWeatherService, WeatherService>();

// Health + Swagger
builder.Services.AddHealthChecks().AddSqlServer(connectionString);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Weather API", Version = "v1" });
    c.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Weather API", Version = "v2" });
});

var app = builder.Build();
// app.UseMiddleware<RequestTrackingMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather API v1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Weather API v2");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    context.Database.EnsureCreated();
}

// ======================
// API v1 endpoints
// ======================
var v1 = app.MapGroup("/api/v1").WithTags("API v1").WithOpenApi();

v1.MapGet("/", () => new
{
    Message = "Welcome to the Weather App!",
    Version = "2.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Database = "Connected"
})
.WithName("GetWelcomeV1");

v1.MapGet("/weather", async (IWeatherService weatherService, int days) =>
{
    var forecasts = await weatherService.GetForecastAsync(days);
    return Results.Ok(forecasts);
})
.WithName("GetWeatherForecastV1");

// ======================
// API v2 endpoints
// ======================
var v2 = app.MapGroup("/api/v2").WithTags("API v2").WithOpenApi();

v2.MapGet("/weather", async (IWeatherService weatherService, int days) =>
{
    var forecasts = await weatherService.GetForecastAsync(days);
    return Results.Ok(new
    {
        Data = forecasts,
        Count = forecasts.Count(),
        Generated = DateTime.UtcNow,
        Version = "v2"
    });
})
.WithName("GetWeatherForecastV2");

// ✅ Secure GET (read access policy)
v2.MapGet("/secure-weather", async (IWeatherService weatherService) =>
{
    var forecasts = await weatherService.GetForecastAsync();
    return Results.Ok(new
    {
        Message = "This is a secure endpoint",
        Data = forecasts,
        AccessTime = DateTime.UtcNow
    });
})
.RequireAuthorization(Policies.RequireWeatherReadAccess)
.WithName("GetSecureWeatherV2");

// ✅ Write-protected POST (write access policy) + stamps CreatedBy
v2.MapPost("/weather", async (
    IWeatherService weatherService,
    WeatherForecast forecast,
    ClaimsPrincipal user,
    Weatherapp.Services.IAuthenticationService authService) =>
{
    forecast.CreatedBy = authService.GetUserIdentity(user);
    var result = await weatherService.AddForecastAsync(forecast);
    return Results.Created($"/api/v2/weather/{result.Id}", result);
})
.RequireAuthorization(Policies.RequireWeatherWriteAccess)
.WithName("AddWeatherForecastV2");

v2.MapGet("/locations", async (IWeatherService weatherService) =>
{
    var locations = await weatherService.GetLocationsAsync();
    return Results.Ok(locations);
})
.WithName("GetLocationsV2");

v2.MapPost("/locations", async (IWeatherService weatherService, WeatherLocation location) =>
{
    var result = await weatherService.AddLocationAsync(location);
    return Results.Created($"/api/v2/locations/{result.Id}", result);
})
.WithName("AddLocationV2");

v2.MapGet("/locations/{locationId}/weather", async (IWeatherService weatherService, int locationId, int days) =>
{
    try
    {
        var forecasts = await weatherService.GetForecastByLocationAsync(locationId, days);
        return Results.Ok(forecasts);
    }
    catch (ArgumentException ex)
    {
        return Results.NotFound(ex.Message);
    }
})
.WithName("GetWeatherByLocationV2");

// Health check endpoint
app.MapHealthChecks("/health").WithTags("Health");

// Backward compatibility - redirect old endpoints
app.MapGet("/", () => Results.Redirect("/api/v1/"));
app.MapGet("/weather", async (IWeatherService weatherService) =>
{
    var forecasts = await weatherService.GetForecastAsync();
    return Results.Ok(forecasts);
});

app.Run();
