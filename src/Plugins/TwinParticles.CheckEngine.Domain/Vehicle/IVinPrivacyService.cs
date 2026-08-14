namespace TwinParticles.CheckEngine.Domain.Vehicle;

/// <summary>
/// Redacts VINs for telemetry, analytics, and audit trails (FR-212, FR-213).
/// </summary>
public interface IVinPrivacyService
{
    string? GetLast4(string? normalizedVin);

    string CreateHash(string normalizedVin);
}
