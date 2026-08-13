using Nop.Core.Configuration;

namespace TwinParticles.CheckEngine.Configuration;

public sealed class CheckEnginePluginSettings : ISettings
{
    public bool Enabled { get; set; } = true;

    public DateTime? UninstallExportPreparedUtc { get; set; }
}
