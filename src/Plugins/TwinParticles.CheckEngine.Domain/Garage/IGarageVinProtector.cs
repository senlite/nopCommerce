namespace TwinParticles.CheckEngine.Domain.Garage;

/// <summary>
/// Protects VINs at the persistence boundary. Domain/application code always works with normalized
/// plaintext; repositories store only the protected representation.
/// </summary>
public interface IGarageVinProtector
{
    string? Protect(string? vin);

    string? Unprotect(string? storedValue);
}
