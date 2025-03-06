using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Auth;

public static class AccessDeniedEndpoint
{
    public static void MapAccessDeniedEndpoint(this WebApplication app)
    {
        app.MapGet("/login/forbidden", [AllowAnonymous]() =>
            Results.Content("Access Denied. You do not have permission to view this page.", "text/html"));
    }
}