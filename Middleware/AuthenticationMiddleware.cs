using MailArchiver.Services;
using Microsoft.AspNetCore.Authentication;

namespace MailArchiver.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, MailArchiver.Services.IAuthenticationService authService)
        {
            // Skip authentication for certain paths
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
            var skipPaths = new[] { "/auth/login", "/auth/logout", "/twofactor/", "/css/", "/js/", "/images/", "/favicon" };
            
            var shouldSkip = skipPaths.Any(skipPath => path.StartsWith(skipPath));

            if (!shouldSkip && authService.IsAuthenticationRequired())
            {
                // Check if user is authenticated through framework
                var isAuthenticated = await context.AuthenticateAsync();
                
                // Also check our custom service for 2FA state
                if (!authService.IsAuthenticated(context))
                {
                    // Store the original URL for redirect after login
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    context.Response.Redirect($"/Auth/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                    return;
                }
            }

            await _next(context);
        }
    }
}
