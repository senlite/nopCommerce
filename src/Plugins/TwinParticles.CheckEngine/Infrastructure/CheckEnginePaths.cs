using Microsoft.AspNetCore.Mvc;

namespace TwinParticles.CheckEngine.Infrastructure;

/// <summary>
/// Plugin HTML shells live under /Admin/CheckEngine/{controller} and /check-engine/{portal}.
/// Conventional RedirectToAction omits those prefixes and 404s browser GETs.
/// </summary>
public static class CheckEnginePaths
{
    public static string Admin(string controller, string action, string? query = null)
    {
        var path = string.Equals(controller, "CheckEngine", System.StringComparison.OrdinalIgnoreCase)
            ? $"/Admin/CheckEngine/{action}"
            : $"/Admin/CheckEngine/{controller}/{action}";
        return AppendQuery(path, query);
    }

    public static string Portal(string portal, string action, string? query = null)
        => AppendQuery($"/check-engine/{portal}/{action}", query);

    public static RedirectResult RedirectAdmin(string controller, string action, string? query = null)
        => new(Admin(controller, action, query));

    public static RedirectResult RedirectPortal(string portal, string action, string? query = null)
        => new(Portal(portal, action, query));

    private static string AppendQuery(string path, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return path;
        return query.StartsWith("?", System.StringComparison.Ordinal) ? path + query : path + "?" + query;
    }
}
