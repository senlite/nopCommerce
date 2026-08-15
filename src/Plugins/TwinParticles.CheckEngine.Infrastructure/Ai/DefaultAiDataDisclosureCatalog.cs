using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class DefaultAiDataDisclosureCatalog : IAiDataDisclosureCatalog
{
    public IReadOnlyList<AiDataDisclosureItem> GetItemsForFeature(string featureKey)
    {
        var common = new List<AiDataDisclosureItem>
        {
            new()
            {
                Category = "Product catalog",
                Description = "Product names, OEM numbers, categories, and attributes from your store catalog.",
                ContainsPersonalData = false
            },
            new()
            {
                Category = "Vehicle reference data",
                Description = "Make, model, generation, and configuration labels from the Check Engine vehicle hierarchy.",
                ContainsPersonalData = false
            }
        };

        if (string.Equals(featureKey, AiFeatureKeys.SearchNaturalLanguage, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(featureKey, AiFeatureKeys.CustomerAssistant, System.StringComparison.OrdinalIgnoreCase))
        {
            common.Add(new AiDataDisclosureItem
            {
                Category = "Customer query text",
                Description = "The search or assistant question typed by the shopper. VINs are redacted to last-4 before transmission.",
                ContainsPersonalData = false
            });
        }

        if (string.Equals(featureKey, AiFeatureKeys.FitmentInference, System.StringComparison.OrdinalIgnoreCase))
        {
            common.Add(new AiDataDisclosureItem
            {
                Category = "Fitment context",
                Description = "Product and vehicle configuration identifiers used to propose a fitment candidate.",
                ContainsPersonalData = false
            });
        }

        return common;
    }
}
