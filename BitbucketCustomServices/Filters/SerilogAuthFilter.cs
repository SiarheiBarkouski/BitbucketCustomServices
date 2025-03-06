using Microsoft.AspNetCore.Authentication;
using Serilog.Ui.Core.Interfaces;

namespace BitbucketCustomServices.Filters;

public class SerilogAuthFilter : IUiAsyncAuthorizationFilter
{
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly HttpContext _httpContext;

    public SerilogAuthFilter(IAuthenticationSchemeProvider schemes,
        IHttpContextAccessor httpContextAccessor)
    {
        _schemes = schemes;
        _httpContext = httpContextAccessor.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor.HttpContext));
    }

    public async Task<bool> AuthorizeAsync()
    {
        var defaultAuthenticate = await _schemes.GetDefaultAuthenticateSchemeAsync();

        if (defaultAuthenticate == null) 
            return false;
        
        var result = await _httpContext.AuthenticateAsync(defaultAuthenticate.Name);

        return result.Succeeded;
    }
}