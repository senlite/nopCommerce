using System;

namespace TwinParticles.CheckEngine.Models;

public sealed class L10nPreviewRequestModel
{
    public string Locale { get; set; } = "en";

    public decimal Number { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string UnitCode { get; set; } = "pcs";
}
