using BitbucketCustomServices.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BitbucketCustomServices.Endpoints.Auth;

public static class LoginEndpoint
{
    public static WebApplication MapLoginEndpoints(this WebApplication app)
    {
        app.MapGet("/login", [AllowAnonymous] async (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            var loginPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "login.html");

            var html = await File.ReadAllTextAsync(loginPath);
            html = html
                .Replace("{{ANTI_FORGERY_FIELD}}", tokens.FormFieldName)
                .Replace("{{ANTI_FORGERY_TOKEN}}", tokens.RequestToken)
                .Replace("{{RETURN_URL}}", context.Request.Query["returnUrl"]);

            return Results.Content(html, "text/html");
        });

        app.MapPost("/login", [AllowAnonymous]
            async (HttpContext context, SignInManager<User> signInManager, UserManager<User> userManager,
                IAntiforgery antiforgery) =>
            {
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                    var form = await context.Request.ReadFormAsync();
                    var username = form["username"];
                    var password = form["password"];

                    var user = await userManager.FindByNameAsync(username);
                    if (user == null)
                    {
                        return Results.Redirect("/login?error=invalid");
                    }

                    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        var returnUrl = form["returnUrl"];

                        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/management" : returnUrl);
                    }

                    return Results.Redirect("/login?error=invalid");
                }
                catch (Exception)
                {
                    return Results.Redirect("/login?error=invalid");
                }
            });

        return app;
    }
}