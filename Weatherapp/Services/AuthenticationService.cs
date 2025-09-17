using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Weatherapp.Services;

public interface IAuthenticationService
{
    Task<bool> IsUserAuthorizedAsync(ClaimsPrincipal user, string permission);
    string GetUserIdentity(ClaimsPrincipal user);
    Dictionary<string, string> GetUserClaims(ClaimsPrincipal user);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(ILogger<AuthenticationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsUserAuthorizedAsync(ClaimsPrincipal user, string permission)
    {
        // Implement your authorization logic here
        // For demo purposes, we'll check if user is authenticated
        return await Task.FromResult(user.Identity?.IsAuthenticated ?? false);
    }

    public string GetUserIdentity(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               user.FindFirst("oid")?.Value ?? 
               "anonymous";
    }

    public Dictionary<string, string> GetUserClaims(ClaimsPrincipal user)
    {
        return user.Claims.ToDictionary(c => c.Type, c => c.Value);
    }
}