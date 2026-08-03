namespace TwinParticles.CheckEngine.Domain.Security;

public interface ICheckEngineInputSanitizer
{
    string SanitizeAlias(string value);
}
