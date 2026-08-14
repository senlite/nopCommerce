namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiFeatureToggle
{
    bool IsEnabled(string featureKey);
}
