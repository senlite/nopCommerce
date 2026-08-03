using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Infrastructure.Security;

public sealed class DefaultCheckEngineInputSanitizer : ICheckEngineInputSanitizer
{
    public string SanitizeAlias(string value)
    {
        return value.Trim();
    }
}
