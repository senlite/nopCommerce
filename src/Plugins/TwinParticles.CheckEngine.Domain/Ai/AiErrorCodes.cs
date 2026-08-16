namespace TwinParticles.CheckEngine.Domain.Ai;

public static class AiErrorCodes
{
    public const string Disabled = "ai.disabled";

    public const string DisclosureRequired = "ai.disclosure_required";

    /// <summary>FR-561 / AiBudgetExceeded — hard daily spend ceiling reached.</summary>
    public const string BudgetExceeded = "ai.budget_exceeded";

    public const string GlobalBudgetExceeded = "ai.global_budget_exceeded";
}
