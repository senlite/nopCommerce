namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IVendorSecretProtector
{
    string? Protect(string? plaintext);
}
