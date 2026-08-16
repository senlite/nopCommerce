namespace TwinParticles.CheckEngine.Models;

public sealed class AssistantRequestModel
{
    public string Question { get; set; } = string.Empty;

    public int? VehicleConfigurationId { get; set; }

    public string? Locale { get; set; }
}

public sealed class FitmentInferenceRequestModel
{
    public int ProductId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public string ProductName { get; set; } = string.Empty;
}

public sealed class AiCandidateReviewModel
{
    public int Id { get; set; }

    public bool Approved { get; set; }
}
