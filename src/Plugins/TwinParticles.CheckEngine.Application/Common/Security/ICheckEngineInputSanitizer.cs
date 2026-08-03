namespace TwinParticles.CheckEngine.Application.Common.Security;

public interface ICheckEngineInputSanitizer
{
    string SanitizeAlias(string value);
}
