using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Application.Oem;

public sealed class OemResolveResult
{
    private OemResolveResult(bool success, string? errorCode)
    {
        Success = success;
        ErrorCode = errorCode;
    }

    public bool Success { get; }

    public string? ErrorCode { get; }

    public int? OemNumberId { get; set; }

    public int? ManufacturerId { get; set; }

    public string? DisplayNumber { get; set; }

    public string? NormalizedNumber { get; set; }

    public bool? IsObsolete { get; set; }

    public int? CurrentOemNumberId { get; set; }

    public IReadOnlyList<int> ProductIds { get; set; } = [];

    public IReadOnlyList<int> CandidateManufacturerIds { get; set; } = [];

    public static OemResolveResult Fail(string errorCode, IReadOnlyList<int>? candidateManufacturerIds = null)
        => new(false, errorCode)
        {
            CandidateManufacturerIds = candidateManufacturerIds ?? []
        };

    public static OemResolveResult Ok() => new(true, null);
}
