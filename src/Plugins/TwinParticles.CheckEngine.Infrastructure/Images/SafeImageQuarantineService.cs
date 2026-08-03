using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Infrastructure.Images;

public sealed class SafeImageQuarantineService : IImageQuarantineService
{
    private static readonly string[] BlockedHostFragments = ["localhost", "127.0.0.1", "169.254.169.254"];

    public Task<bool> ShouldQuarantineAsync(string sourceUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            return Task.FromResult(false);

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            return Task.FromResult(true);

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return Task.FromResult(true);

        foreach (var blocked in BlockedHostFragments)
        {
            if (uri.Host.Contains(blocked, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
