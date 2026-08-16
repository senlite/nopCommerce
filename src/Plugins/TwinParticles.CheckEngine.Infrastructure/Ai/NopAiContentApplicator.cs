using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

/// <summary>
/// Maps approved AI generation candidates onto nopCommerce product fields.
/// </summary>
public sealed class NopAiContentApplicator : IAiContentApplicator
{
    private readonly IProductService _productService;
    private readonly ILocalizedEntityService _localizedEntityService;
    private readonly ILanguageService _languageService;
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public NopAiContentApplicator(
        IProductService productService,
        ILocalizedEntityService localizedEntityService,
        ILanguageService languageService,
        ISpecificationAttributeService specificationAttributeService)
    {
        _productService = productService;
        _localizedEntityService = localizedEntityService;
        _languageService = languageService;
        _specificationAttributeService = specificationAttributeService;
    }

    public async Task<AiContentApplyResult> ApplyAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (candidate.EntityId <= 0 || string.IsNullOrWhiteSpace(candidate.OutputText))
            return AiContentApplyResult.Fail("ai.apply.invalid_candidate");

        var product = await _productService.GetProductByIdAsync(candidate.EntityId);
        if (product is null || product.Deleted)
            return AiContentApplyResult.Fail("ai.apply.product_not_found");

        return candidate.EntityType switch
        {
            AiGenerationEntityType.ProductDescription => await ApplyDescriptionAsync(product, candidate, cancellationToken),
            AiGenerationEntityType.Specification => await ApplySpecificationAsync(product, candidate, cancellationToken),
            AiGenerationEntityType.Translation => await ApplyTranslationAsync(product, candidate, cancellationToken),
            AiGenerationEntityType.SeoMetadata => await ApplySeoAsync(product, candidate, cancellationToken),
            _ => AiContentApplyResult.Fail("ai.apply.unsupported_entity_type")
        };
    }

    private async Task<AiContentApplyResult> ApplyDescriptionAsync(
        Product product,
        AiGenerationCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (await TryApplyLocalizedAsync(product, candidate, p => p.FullDescription, cancellationToken))
            return AiContentApplyResult.Ok();

        product.FullDescription = candidate.OutputText.Trim();
        await _productService.UpdateProductAsync(product);
        return AiContentApplyResult.Ok();
    }

    private async Task<AiContentApplyResult> ApplySpecificationAsync(
        Product product,
        AiGenerationCandidate candidate,
        CancellationToken cancellationToken)
    {
        var lines = ParseSpecificationLines(candidate.OutputText);
        if (lines.Count == 0)
        {
            product.ShortDescription = candidate.OutputText.Trim();
            await _productService.UpdateProductAsync(product);
            return AiContentApplyResult.Ok();
        }

        var attributePage = await _specificationAttributeService.GetAllSpecificationAttributesAsync();
        var attributes = attributePage.ToList();
        var existingProductSpecs = await _specificationAttributeService.GetProductSpecificationAttributesAsync(product.Id);
        var displayOrder = existingProductSpecs.Count;
        var applied = 0;

        foreach (var (key, value) in lines)
        {
            var attribute = attributes.FirstOrDefault(x =>
                x.Name.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (attribute is null)
            {
                attribute = new SpecificationAttribute
                {
                    Name = key,
                    DisplayOrder = attributes.Count + applied + 1
                };
                await _specificationAttributeService.InsertSpecificationAttributeAsync(attribute);
                attributes.Add(attribute);
            }

            var productSpecification = new ProductSpecificationAttribute
            {
                ProductId = product.Id,
                AttributeType = SpecificationAttributeType.CustomText,
                SpecificationAttributeOptionId = 0,
                CustomValue = value,
                AllowFiltering = false,
                ShowOnProductPage = true,
                DisplayOrder = displayOrder++
            };

            await _specificationAttributeService.InsertProductSpecificationAttributeAsync(productSpecification);
            applied++;
        }

        return applied > 0
            ? AiContentApplyResult.Ok()
            : AiContentApplyResult.Fail("ai.apply.specification_empty");
    }

    private async Task<AiContentApplyResult> ApplyTranslationAsync(
        Product product,
        AiGenerationCandidate candidate,
        CancellationToken cancellationToken)
    {
        var languageId = await ResolveLanguageIdAsync(candidate.Locale, cancellationToken);
        if (languageId is null)
            return AiContentApplyResult.Fail("ai.apply.language_not_found");

        await _localizedEntityService.SaveLocalizedValueAsync(product, p => p.Name, candidate.OutputText.Trim(), languageId.Value);
        return AiContentApplyResult.Ok();
    }

    private async Task<AiContentApplyResult> ApplySeoAsync(
        Product product,
        AiGenerationCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (!TryParseSeoPayload(candidate.OutputText, out var metaTitle, out var metaDescription))
            return AiContentApplyResult.Fail("ai.apply.seo_parse_failed");

        var languageId = await ResolveLanguageIdAsync(candidate.Locale, cancellationToken);
        if (languageId is null)
        {
            if (!string.IsNullOrWhiteSpace(metaTitle))
                product.MetaTitle = metaTitle;
            if (!string.IsNullOrWhiteSpace(metaDescription))
                product.MetaDescription = metaDescription;

            await _productService.UpdateProductAsync(product);
            return AiContentApplyResult.Ok();
        }

        if (!string.IsNullOrWhiteSpace(metaTitle))
            await _localizedEntityService.SaveLocalizedValueAsync(product, p => p.MetaTitle, metaTitle, languageId.Value);
        if (!string.IsNullOrWhiteSpace(metaDescription))
            await _localizedEntityService.SaveLocalizedValueAsync(product, p => p.MetaDescription, metaDescription, languageId.Value);

        return AiContentApplyResult.Ok();
    }

    private async Task<bool> TryApplyLocalizedAsync(
        Product product,
        AiGenerationCandidate candidate,
        Expression<Func<Product, string>> selector,
        CancellationToken cancellationToken)
    {
        var languageId = await ResolveLanguageIdAsync(candidate.Locale, cancellationToken);
        if (languageId is null)
            return false;

        await _localizedEntityService.SaveLocalizedValueAsync(product, selector, candidate.OutputText.Trim(), languageId.Value);
        return true;
    }

    private async Task<int?> ResolveLanguageIdAsync(string locale, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(locale) || locale.Equals("en", StringComparison.OrdinalIgnoreCase))
            return null;

        var languages = await _languageService.GetAllLanguagesAsync(showHidden: true);
        foreach (var language in languages)
        {
            if (language.UniqueSeoCode.Equals(locale, StringComparison.OrdinalIgnoreCase))
                return language.Id;
        }

        return null;
    }

    private static bool TryParseSeoPayload(string outputText, out string? metaTitle, out string? metaDescription)
    {
        metaTitle = null;
        metaDescription = null;

        var trimmed = outputText.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                var root = document.RootElement;
                metaTitle = ReadJsonString(root, "metaTitle") ?? ReadJsonString(root, "title");
                metaDescription = ReadJsonString(root, "metaDescription") ?? ReadJsonString(root, "description");
                return !string.IsNullOrWhiteSpace(metaTitle) || !string.IsNullOrWhiteSpace(metaDescription);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        var lines = trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length >= 2)
        {
            metaTitle = lines[0];
            metaDescription = lines[1];
            return true;
        }

        metaDescription = trimmed;
        return true;
    }

    private static string? ReadJsonString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            return null;

        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyList<(string Key, string Value)> ParseSpecificationLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => (parts[0].Trim(), parts[1].Trim()))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Item1) && !string.IsNullOrWhiteSpace(pair.Item2))
            .ToList();
    }
}
