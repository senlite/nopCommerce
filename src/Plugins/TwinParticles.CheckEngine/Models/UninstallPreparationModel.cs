using System;

namespace TwinParticles.CheckEngine.Models;

public sealed class UninstallPreparationModel
{
    public bool Compact { get; init; }

    public bool InterceptPluginListUninstall { get; init; }

    public string Warning { get; init; } = string.Empty;

    public string ExportRequired { get; init; } = string.Empty;

    public string ExportPrepared { get; init; } = string.Empty;

    public string ExportExpired { get; init; } = string.Empty;

    public string ExportAction { get; init; } = string.Empty;

    public string ConfirmTitle { get; init; } = string.Empty;

    public string ConfirmBody { get; init; } = string.Empty;

    public string ConfirmProceed { get; init; } = string.Empty;

    public string ConfirmCancel { get; init; } = string.Empty;

    public string ExportUrl { get; init; } = "/Admin/CheckEngine/UninstallAdmin/Export";

    public bool Prepared { get; init; }

    public DateTime? PreparedUtc { get; init; }

    public DateTime? ExpiresUtc { get; init; }
}
