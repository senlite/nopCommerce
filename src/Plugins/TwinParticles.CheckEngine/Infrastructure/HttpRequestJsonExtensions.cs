using Microsoft.AspNetCore.Http;

namespace TwinParticles.CheckEngine.Infrastructure;

/// <summary>
/// Distinguishes API/fetch requests from browser navigation so JSON endpoints can serve HTML shells.
/// </summary>
public static class HttpRequestJsonExtensions
{
    public static bool WantsJsonResponse(this HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Requested-With", out var xhr)
            && xhr.ToString().Contains("XMLHttpRequest", System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (request.Query.ContainsKey("json"))
            return true;

        var accept = request.Headers.Accept.ToString();
        if (string.IsNullOrWhiteSpace(accept))
            return false;

        if (accept.Contains("text/html", System.StringComparison.OrdinalIgnoreCase)
            && !accept.Contains("application/json", System.StringComparison.OrdinalIgnoreCase))
            return false;

        return accept.Contains("application/json", System.StringComparison.OrdinalIgnoreCase);
    }
}
