using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Panelarr.Http.Middleware
{
    public class LoginRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public LoginRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // If the response is 401/403 and it's a browser request (not API/AJAX),
            // redirect to login instead of returning the error
            if ((context.Response.StatusCode == 401 || context.Response.StatusCode == 403) &&
                !context.Response.HasStarted &&
                IsBrowserRequest(context.Request))
            {
                var returnUrl = context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"/login?returnUrl={System.Net.WebUtility.UrlEncode(returnUrl)}");
            }
        }

        private static bool IsBrowserRequest(HttpRequest request)
        {
            // API requests use X-Api-Key header or have apikey query param
            if (request.Headers.ContainsKey("X-Api-Key") ||
                request.Query.ContainsKey("apikey"))
            {
                return false;
            }

            // AJAX requests from the frontend
            if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return false;
            }

            // API paths
            if (request.Path.StartsWithSegments("/api") ||
                request.Path.StartsWithSegments("/feed") ||
                request.Path.StartsWithSegments("/ping"))
            {
                return false;
            }

            // If Accept header includes text/html, it's likely a browser
            var accept = request.Headers["Accept"].ToString();
            if (accept.Contains("text/html") || accept.Contains("*/*") || string.IsNullOrEmpty(accept))
            {
                return true;
            }

            return false;
        }
    }
}
