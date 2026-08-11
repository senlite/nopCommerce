using System.Net;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Media;
using Nop.Services.Seo;

namespace Nop.Plugin.Misc.GMaster.Services;

public sealed class GMasterCatalogImportService
{
    private readonly ICategoryService _categoryService;
    private readonly ICategoryTemplateService _categoryTemplateService;
    private readonly CurrencySettings _currencySettings;
    private readonly ICurrencyService _currencyService;
    private readonly GMasterCatalogParser _catalogParser;
    private readonly GMasterImageFactory _imageFactory;
    private readonly GMasterSettings _settings;
    private readonly IManufacturerService _manufacturerService;
    private readonly IManufacturerTemplateService _manufacturerTemplateService;
    private readonly INopFileProvider _fileProvider;
    private readonly IPictureService _pictureService;
    private readonly IProductService _productService;
    private readonly IProductTemplateService _productTemplateService;
    private readonly ISettingService _settingService;
    private readonly IUrlRecordService _urlRecordService;

    public GMasterCatalogImportService(
        ICategoryService categoryService,
        ICategoryTemplateService categoryTemplateService,
        CurrencySettings currencySettings,
        ICurrencyService currencyService,
        GMasterCatalogParser catalogParser,
        GMasterImageFactory imageFactory,
        GMasterSettings settings,
        IManufacturerService manufacturerService,
        IManufacturerTemplateService manufacturerTemplateService,
        INopFileProvider fileProvider,
        IPictureService pictureService,
        IProductService productService,
        IProductTemplateService productTemplateService,
        ISettingService settingService,
        IUrlRecordService urlRecordService)
    {
        _categoryService = categoryService;
        _categoryTemplateService = categoryTemplateService;
        _currencySettings = currencySettings;
        _currencyService = currencyService;
        _catalogParser = catalogParser;
        _imageFactory = imageFactory;
        _settings = settings;
        _manufacturerService = manufacturerService;
        _manufacturerTemplateService = manufacturerTemplateService;
        _fileProvider = fileProvider;
        _pictureService = pictureService;
        _productService = productService;
        _productTemplateService = productTemplateService;
        _settingService = settingService;
        _urlRecordService = urlRecordService;
    }

    /// <summary>
    /// Validates the source, stages a complete unpublished replacement, then soft-deletes the previous
    /// active catalog and publishes the replacement. Soft-delete intentionally preserves historical
    /// order integrity; staging first avoids a blank storefront if creation fails.
    /// </summary>
    public async Task<GMasterCatalogImportResult> ReplaceCatalogAsync(CancellationToken cancellationToken = default)
    {
        var csvPath = _fileProvider.MapPath($"~/{GMasterDefaults.CatalogRelativePath}");
        if (!_fileProvider.FileExists(csvPath))
            throw new FileNotFoundException($"GMaster catalog data not found at '{csvPath}'.", csvPath);

        var csv = await _fileProvider.ReadAllTextAsync(csvPath, Encoding.UTF8);
        var items = _catalogParser.Parse(csv);
        ValidateCategories(items);

        var categoryTemplate = (await _categoryTemplateService.GetAllCategoryTemplatesAsync()).FirstOrDefault()
            ?? throw new InvalidOperationException("nopCommerce has no category template.");
        var productTemplate = (await _productTemplateService.GetAllProductTemplatesAsync()).FirstOrDefault()
            ?? throw new InvalidOperationException("nopCommerce has no product template.");
        var manufacturerTemplate = (await _manufacturerTemplateService.GetAllManufacturerTemplatesAsync()).FirstOrDefault()
            ?? throw new InvalidOperationException("nopCommerce has no manufacturer template.");

        var existingProducts = await _productService.SearchProductsAsync(
            pageSize: int.MaxValue,
            showHidden: true,
            overridePublished: null);
        var existingCategories = await _categoryService.GetAllCategoriesAsync(showHidden: true);

        // Stage the entire replacement catalog unpublished. If staging fails, the currently published
        // catalog remains online; a later successful run will clean up the unpublished partial stage.
        var pictures = await CreateCategoryPicturesAsync();
        var root = await CreateRootCategoryAsync(categoryTemplate.Id, pictures["body-underbody"], published: false);
        var categories = await CreateCategoriesAsync(root.Id, categoryTemplate.Id, pictures);
        var manufacturer = await CreateManufacturerAsync(manufacturerTemplate.Id, pictures["exterior-grilles"]);
        var importedProducts = new List<Product>(items.Count);

        var displayOrder = 0;
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var product = CreateProduct(item, productTemplate.Id, displayOrder);
            await _productService.InsertProductAsync(product);
            importedProducts.Add(product);

            await _categoryService.InsertProductCategoryAsync(new ProductCategory
            {
                ProductId = product.Id,
                CategoryId = categories[item.CategoryKey].Id,
                DisplayOrder = displayOrder
            });

            await _manufacturerService.InsertProductManufacturerAsync(new ProductManufacturer
            {
                ProductId = product.Id,
                ManufacturerId = manufacturer.Id,
                DisplayOrder = 0
            });

            await _productService.InsertProductPictureAsync(new ProductPicture
            {
                ProductId = product.Id,
                PictureId = pictures[item.CategoryKey],
                DisplayOrder = 0
            });

            var productSlug = await _urlRecordService.ValidateSeNameAsync(
                product,
                $"gmaster-{item.Sku}",
                item.EnglishName,
                ensureNotEmpty: true);
            await _urlRecordService.SaveSlugAsync(product, productSlug, 0);

            displayOrder++;
        }

        // Destructive cutover is deliberate and is the plugin's core contract.
        foreach (var product in existingProducts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _productService.DeleteProductAsync(product);
        }

        // Delete deepest categories first so parents do not have to be reparented repeatedly.
        foreach (var category in existingCategories.OrderByDescending(category => category.ParentCategoryId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _categoryService.DeleteCategoryAsync(category);
        }

        root.Published = true;
        root.UpdatedOnUtc = DateTime.UtcNow;
        await _categoryService.UpdateCategoryAsync(root);
        foreach (var category in categories.Values)
        {
            category.Published = true;
            category.UpdatedOnUtc = DateTime.UtcNow;
            await _categoryService.UpdateCategoryAsync(category);
        }

        foreach (var product in importedProducts)
        {
            product.Published = true;
            product.UpdatedOnUtc = DateTime.UtcNow;
            await _productService.UpdateProductAsync(product);
        }

        await ConfigureEgyptianPoundAsync();

        var completedUtc = DateTime.UtcNow;
        _settings.LastImportUtc = completedUtc;
        _settings.ImportedProductCount = items.Count;
        _settings.ImportedCategoryCount = categories.Count + 1;
        _settings.ClearedProductCount = existingProducts.Count;
        _settings.ClearedCategoryCount = existingCategories.Count;
        _settings.CatalogSourceVersion = GMasterDefaults.SourceVersion;
        _settings.LastError = string.Empty;
        await _settingService.SaveSettingAsync(_settings);

        return new GMasterCatalogImportResult(
            existingProducts.Count,
            existingCategories.Count,
            items.Count,
            categories.Count + 1,
            completedUtc);
    }

    public static decimal CalculateSellingPrice(decimal cost)
    {
        var multiplier = cost switch
        {
            <= 500m => 1.55m,
            <= 1_000m => 1.45m,
            <= 3_000m => 1.35m,
            <= 10_000m => 1.28m,
            _ => 1.22m
        };

        return Math.Ceiling(cost * multiplier / 10m) * 10m;
    }

    private static void ValidateCategories(IReadOnlyList<GMasterCatalogItem> items)
    {
        var allowed = GMasterCategoryCatalog.All.Select(category => category.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unknown = items.Select(item => item.CategoryKey)
            .Where(key => !allowed.Contains(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unknown.Count > 0)
            throw new InvalidOperationException($"Unknown category keys: {string.Join(", ", unknown)}.");

        foreach (var item in items)
        {
            var expectedSellingPrice = CalculateSellingPrice(item.CostPrice);
            if (item.SellingPrice != expectedSellingPrice)
            {
                throw new InvalidOperationException(
                    $"SKU '{item.Sku}' has selling price {item.SellingPrice}; expected {expectedSellingPrice} from the configured Egyptian-market margin policy.");
            }
        }
    }

    private async Task ConfigureEgyptianPoundAsync()
    {
        var currency = await _currencyService.GetCurrencyByCodeAsync("EGP");
        if (currency is null)
        {
            currency = new Currency
            {
                Name = "Egyptian pound",
                CurrencyCode = "EGP",
                Rate = 1m,
                DisplayLocale = "ar-EG",
                CustomFormatting = "ج.م. 0.00",
                Published = true,
                DisplayOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _currencyService.InsertCurrencyAsync(currency);
        }
        else
        {
            currency.Published = true;
            currency.Rate = 1m;
            currency.CustomFormatting = "ج.م. 0.00";
            currency.UpdatedOnUtc = DateTime.UtcNow;
            await _currencyService.UpdateCurrencyAsync(currency);
        }

        // The imported price list and calculated selling prices are EGP amounts. Leaving another
        // storefront currency active would display those numbers under the wrong symbol/rate.
        foreach (var otherCurrency in (await _currencyService.GetAllCurrenciesAsync(showHidden: true))
                     .Where(item => item.Id != currency.Id && item.Published))
        {
            otherCurrency.Published = false;
            otherCurrency.UpdatedOnUtc = DateTime.UtcNow;
            await _currencyService.UpdateCurrencyAsync(otherCurrency);
        }

        _currencySettings.PrimaryStoreCurrencyId = currency.Id;
        _currencySettings.PrimaryExchangeRateCurrencyId = currency.Id;
        _currencySettings.DisplayCurrencyLabel = true;
        await _settingService.SaveSettingAsync(_currencySettings);
    }

    private async Task<Dictionary<string, int>> CreateCategoryPicturesAsync()
    {
        var pictures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in GMasterCategoryCatalog.All)
        {
            var binary = _imageFactory.CreateCategorySvg(definition);
            var seoName = await _pictureService.GetPictureSeNameAsync($"gmaster-{definition.Key}");
            var picture = await _pictureService.InsertPictureAsync(
                binary,
                MimeTypes.ImageSvg,
                seoName,
                $"{definition.ArabicName} - {definition.EnglishName}",
                definition.EnglishName,
                validateBinary: false);
            pictures[definition.Key] = picture.Id;
        }

        return pictures;
    }

    private async Task<Category> CreateRootCategoryAsync(int templateId, int pictureId, bool published)
    {
        var category = new Category
        {
            Name = "قطع غيار وإكسسوارات BMW | GMaster BMW Parts",
            Description = "كتالوج GMaster لقطع الغيار والإكسسوارات المتوافقة مع سيارات BMW.",
            CategoryTemplateId = templateId,
            PictureId = pictureId,
            PageSize = 24,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "12,24,48",
            ShowOnHomepage = true,
            IncludeInTopMenu = true,
            Published = published,
            DisplayOrder = 1,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };
        await _categoryService.InsertCategoryAsync(category);
        var slug = await _urlRecordService.ValidateSeNameAsync(category, "gmaster-bmw-parts", category.Name, true);
        await _urlRecordService.SaveSlugAsync(category, slug, 0);
        return category;
    }

    private async Task<Dictionary<string, Category>> CreateCategoriesAsync(
        int rootCategoryId,
        int templateId,
        IReadOnlyDictionary<string, int> pictures)
    {
        var categories = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        var order = 0;
        foreach (var definition in GMasterCategoryCatalog.All)
        {
            var category = new Category
            {
                Name = $"{definition.ArabicName} | {definition.EnglishName}",
                Description = definition.Description,
                CategoryTemplateId = templateId,
                ParentCategoryId = rootCategoryId,
                PictureId = pictures[definition.Key],
                PageSize = 24,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12,24,48",
                ShowOnHomepage = false,
                IncludeInTopMenu = false,
                Published = false,
                DisplayOrder = order++,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _categoryService.InsertCategoryAsync(category);
            var slug = await _urlRecordService.ValidateSeNameAsync(
                category,
                $"gmaster-{definition.Key}",
                definition.EnglishName,
                true);
            await _urlRecordService.SaveSlugAsync(category, slug, 0);
            categories[definition.Key] = category;
        }

        return categories;
    }

    private async Task<Manufacturer> CreateManufacturerAsync(int templateId, int pictureId)
    {
        var manufacturer = (await _manufacturerService.GetAllManufacturersAsync(
                manufacturerName: "GMaster",
                showHidden: true))
            .FirstOrDefault(manufacturer =>
                manufacturer.Name.Equals("GMaster", StringComparison.OrdinalIgnoreCase));

        var isNew = manufacturer is null;
        manufacturer ??= new Manufacturer
        {
            Name = "GMaster",
            PageSize = 24,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "12,24,48",
            DisplayOrder = 1,
            CreatedOnUtc = DateTime.UtcNow
        };

        manufacturer.Description = "GMaster curated BMW-compatible replacement parts and accessories.";
        manufacturer.ManufacturerTemplateId = templateId;
        manufacturer.PictureId = pictureId;
        manufacturer.Published = true;
        manufacturer.Deleted = false;
        manufacturer.UpdatedOnUtc = DateTime.UtcNow;

        if (isNew)
            await _manufacturerService.InsertManufacturerAsync(manufacturer);
        else
            await _manufacturerService.UpdateManufacturerAsync(manufacturer);

        var slug = await _urlRecordService.ValidateSeNameAsync(manufacturer, "gmaster", manufacturer.Name, true);
        await _urlRecordService.SaveSlugAsync(manufacturer, slug, 0);
        return manufacturer;
    }

    private static Product CreateProduct(GMasterCatalogItem item, int templateId, int displayOrder)
    {
        var safeArabic = WebUtility.HtmlEncode(item.ArabicName);
        var safeEnglish = WebUtility.HtmlEncode(item.EnglishName);
        var safeOem = WebUtility.HtmlEncode(item.Oem);
        var safeModels = WebUtility.HtmlEncode(item.VehicleModels);
        var margin = item.SellingPrice - item.CostPrice;
        var marginPercent = Math.Round(margin / item.CostPrice * 100m, 1);

        var oemLine = string.IsNullOrWhiteSpace(item.Oem)
            ? string.Empty
            : $"<li><strong>OEM / Item code:</strong> <span dir=\"ltr\">{safeOem}</span></li>";
        var modelLine = string.IsNullOrWhiteSpace(item.VehicleModels)
            ? string.Empty
            : $"<li><strong>Compatible chassis:</strong> <span dir=\"ltr\">{safeModels}</span></li>";

        return new Product
        {
            ProductType = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Name = $"{item.ArabicName} | {item.EnglishName}",
            Sku = item.Sku,
            ManufacturerPartNumber = item.Oem,
            ShortDescription = $"{item.ArabicName} — متوافق مع {item.VehicleModels}.",
            FullDescription = $"""
                <div dir="auto">
                  <h2>{safeArabic}</h2>
                  <p lang="en">{safeEnglish}</p>
                  <ul>
                    {oemLine}
                    {modelLine}
                    <li><strong>Brand:</strong> GMaster</li>
                    <li><strong>Source list:</strong> {WebUtility.HtmlEncode(item.SourceFile)}</li>
                  </ul>
                  <p><small>السعر المعروض هو سعر البيع. تكلفة المورد محفوظة في حقل تكلفة المنتج داخل لوحة الإدارة.</small></p>
                </div>
                """,
            AdminComment = $"Imported by GMaster. Cost EGP {item.CostPrice:0.##}; selling EGP {item.SellingPrice:0.##}; gross margin {marginPercent:0.#}%. Source {item.SourceFile}.",
            ProductTemplateId = templateId,
            AllowCustomerReviews = true,
            Price = item.SellingPrice,
            ProductCost = item.CostPrice,
            IsShipEnabled = true,
            Weight = 1m,
            Length = 30m,
            Width = 20m,
            Height = 15m,
            ManageInventoryMethod = ManageInventoryMethod.DontManageStock,
            DisplayStockAvailability = false,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 100,
            Published = false,
            ShowOnHomepage = displayOrder < 12,
            MarkAsNew = true,
            MetaTitle = $"{item.ArabicName} | GMaster",
            MetaDescription = $"{item.ArabicName} - {item.EnglishName}. متوافق مع {item.VehicleModels}.",
            MetaKeywords = $"{item.Oem},{item.VehicleModels},BMW,GMaster",
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };
    }
}
