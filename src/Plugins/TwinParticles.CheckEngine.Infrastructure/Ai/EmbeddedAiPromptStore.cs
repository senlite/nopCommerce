using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

/// <summary>
/// Versioned prompt catalog shipped with the plugin (FR-563).
/// </summary>
public sealed class EmbeddedAiPromptStore : IAiPromptStore
{
    private static readonly IReadOnlyDictionary<string, AiPromptDefinition> Catalog =
        new Dictionary<string, AiPromptDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [AiFeatureKeys.ImportEnrichment] = new()
            {
                PromptKey = AiFeatureKeys.ImportEnrichment,
                Version = "1",
                Body = "Write a concise automotive product description. Do not invent OEM numbers, fitment, price, or stock. Name={name}; OEM={oem}."
            },
            [AiFeatureKeys.ImportSpecification] = new()
            {
                PromptKey = AiFeatureKeys.ImportSpecification,
                Version = "1",
                Body = """
                    Extract automotive product specifications as a JSON array of objects with keys "key" and "value".
                    Only use these attribute keys when applicable: Material, Thread, Thread Size, Diameter, Length, Width, Height, Weight, Color, Finish, Voltage, Amperage, Capacity, Volume, Viscosity, Position, Side, Drive Type, Transmission, Engine Code, OEM Number, Manufacturer, Warranty, Quantity, Package Contents, Includes, Notes.
                    Only extract facts implied by the product name and OEM; do not invent dimensions, fitment, or compatibility.
                    Return only JSON. Name={name}; OEM={oem}
                    """
            },
            [AiFeatureKeys.ImportTranslation] = new()
            {
                PromptKey = AiFeatureKeys.ImportTranslation,
                Version = "1",
                Body = "Translate this automotive product name to {targetLocale}. Use the controlled glossary when provided. Return only the translation. Source={sourceText}\n{glossaryContext}"
            },
            [AiFeatureKeys.ImportSeo] = new()
            {
                PromptKey = AiFeatureKeys.ImportSeo,
                Version = "1",
                Body = "Generate SEO metadata as JSON with keys metaTitle and metaDescription for this product. Return only JSON. Name={name}"
            },
            [AiFeatureKeys.SearchNaturalLanguage] = new()
            {
                PromptKey = AiFeatureKeys.SearchNaturalLanguage,
                Version = "1",
                Body = """
                    Parse this automotive search query into JSON with keys:
                    partTerms (string array), make, model, modelYear (number or null), oemNumber, keywordFallback.
                    Return only JSON. Locale={locale}. Query: {query}
                    """
            },
            [AiFeatureKeys.CustomerAssistant] = new()
            {
                PromptKey = AiFeatureKeys.CustomerAssistant,
                Version = "1",
                Body = "Answer using only the catalog context below. Refuse if unsure. Do not invent OEM, price, stock, or fitment.\nContext:\n{context}\nQuestion: {question}"
            },
            [AiFeatureKeys.FitmentInference] = new()
            {
                PromptKey = AiFeatureKeys.FitmentInference,
                Version = "1",
                Body = "Infer fitment claims as JSON array with productId, configurationId, verdict, confidence, rationale. Never auto-publish. Product={productId}; Vehicle={configurationId}; Attributes={attributes}"
            },
            [AiFeatureKeys.ContentDescription] = new()
            {
                PromptKey = AiFeatureKeys.ContentDescription,
                Version = "1",
                Body = "Write a concise automotive product description. Do not invent OEM numbers, fitment, price, or stock. Name={name}; OEM={oem}."
            }
        };

    public AiPromptDefinition? Resolve(string promptKey, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(promptKey))
            return null;

        return Catalog.TryGetValue(promptKey, out var definition) ? definition : null;
    }
}
