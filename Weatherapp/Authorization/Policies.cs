using Microsoft.AspNetCore.Authorization;

namespace Weatherapp.Authorization;

public static class Policies
{
    public const string RequireWeatherReadAccess = "RequireWeatherReadAccess";
    public const string RequireWeatherWriteAccess = "RequireWeatherWriteAccess";
    public const string RequireAdminAccess = "RequireAdminAccess";
}

public class WeatherReadRequirement : IAuthorizationRequirement
{
}

public class WeatherWriteRequirement : IAuthorizationRequirement
{
}

public class AdminRequirement : IAuthorizationRequirement
{
}

public class WeatherAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        if (requirement is WeatherReadRequirement)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                context.Succeed(requirement);
            }
        }
        else if (requirement is WeatherWriteRequirement)
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                (context.User.HasClaim("roles", "WeatherWriter") || context.User.HasClaim("roles", "Admin")))
            {
                context.Succeed(requirement);
            }
        }
        else if (requirement is AdminRequirement)
        {
            if (context.User.HasClaim("roles", "Admin"))
            {
                context.Succeed(requirement);
            }
        }
        return Task.CompletedTask;
    }
}