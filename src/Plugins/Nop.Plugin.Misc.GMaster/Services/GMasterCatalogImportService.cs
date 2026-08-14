using System.Net;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Media;
using Nop.Services.Seo;

namespace Nop.Plugin.Misc.GMaster.Services;

public sealed class GMasterCatalogImportService
{
    /// <summary>Maximum photos attached per product.</summary>
    public const int MaxPicturesPerProduct = 3;

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
    private readonly IRepository<ShoppingCartItem> _shoppingCartItemRepository;
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
        IRepository<ShoppingCartItem> shoppingCartItemRepository,
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
        _shoppingCartItemRepository = shoppingCartItemRepository;
        _urlRecordService = urlRecordService;
    }

    /// <summary>
    /// Validates the source, stages a complete unpublished replacement, publishes it, then soft-deletes
    /// the previous active catalog. Soft-delete preserves historical order integrity; publish-first
    /// cutover means a failure can temporarily overlap catalogs but can never blank the storefront.
    /// </summary>
    public async Task<GMasterCatalogImportResult> ReplaceCatalogAsync(CancellationToken cancellationToken = default)
    {
        var csvPath = _fileProvider.MapPath($"~/{GMasterDefaults.CatalogRelativePath}");
        if (!_fileProvider.FileExists(csvPath))
            throw new FileNotFoundException($"GMaster catalog data not found at '{csvPath}'.", csvPath);

        var csv = await _fileProvider.ReadAllTextAsync(csvPath, Encoding.UTF8);
        var items = _catalogParser.Parse(csv);
        ValidateCategories(items);

        var rmbToEgp = _settings.RmbToEgpRate > 0 ? _settings.RmbToEgpRate : GMasterDefaults.DefaultRmbToEgpRate;
        var imageDirectory = _fileProvider.MapPath($"~/{GMasterDefaults.ImageRelativeDirectory}");

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
        var brandingPictureId = pictures.Values.First();
        var root = await CreateRootCategoryAsync(categoryTemplate.Id, brandingPictureId, published: false);
        var categories = await CreateCategoriesAsync(root.Id, categoryTemplate.Id, pictures);
        var manufacturer = await CreateManufacturerAsync(manufacturerTemplate.Id, brandingPictureId);
        var importedProducts = new List<Product>(items.Count);
        var importedImages = 0;

        var displayOrder = 0;
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var costEgp = Math.Round(item.CostRmb * rmbToEgp, 2, MidpointRounding.AwayFromZero);
            var sellingEgp = CalculateSellingPrice(costEgp);

            var product = CreateProduct(item, productTemplate.Id, displayOrder, costEgp, sellingEgp);
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

            // Prefer the real supplier photos; fall back to the category illustration when a row has none.
            var productPictureIds = await CreateProductPicturesAsync(imageDirectory, item);
            importedImages += productPictureIds.Count;
            if (productPictureIds.Count == 0)
                productPictureIds = [pictures[item.CategoryKey]];

            for (var pictureIndex = 0; pictureIndex < productPictureIds.Count; pictureIndex++)
            {
                await _productService.InsertProductPictureAsync(new ProductPicture
                {
                    ProductId = product.Id,
                    PictureId = productPictureIds[pictureIndex],
                    DisplayOrder = pictureIndex
                });
            }

            var productSlug = await _urlRecordService.ValidateSeNameAsync(
                product,
                $"gmaster-{item.Sku}",
                item.EnglishName,
                ensureNotEmpty: true);
            await _urlRecordService.SaveSlugAsync(product, productSlug, 0);

            displayOrder++;
        }

        // Cutover starts by publishing the complete staged replacement. If any publish operation fails,
        // the old catalog is still online; a failure can produce overlap, but never a blank storefront.
        // Do not observe request cancellation after this point: client disconnects must not interrupt a
        // destructive cutover halfway through.
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

        // Cart and wishlist rows point to the previous product ids. Clear them before deleting those
        // products so customers never see "Product is deleted" rows.
        var existingCartItems = await _shoppingCartItemRepository.GetAllAsync(query => query);
        if (existingCartItems.Count > 0)
            await _shoppingCartItemRepository.DeleteAsync(existingCartItems, publishEvent: false);

        foreach (var product in existingProducts)
            await _productService.DeleteProductAsync(product);

        // Child categories first minimizes reparenting churn; this is a two-level-safe preference,
        // not a reliance on database identifier order.
        foreach (var category in existingCategories.OrderByDescending(category => category.ParentCategoryId != 0))
            await _categoryService.DeleteCategoryAsync(category);

        var completedUtc = DateTime.UtcNow;
        _settings.LastImportUtc = completedUtc;
        _settings.ImportedProductCount = items.Count;
        _settings.ImportedCategoryCount = categories.Count + 1;
        _settings.ImportedImageCount = importedImages;
        _settings.ClearedProductCount = existingProducts.Count;
        _settings.ClearedCategoryCount = existingCategories.Count;
        _settings.ClearedCartItemCount = existingCartItems.Count;
        _settings.RmbToEgpRate = rmbToEgp;
        _settings.CatalogSourceVersion = GMasterDefaults.SourceVersion;
        _settings.LastError = string.Empty;
        await _settingService.SaveSettingAsync(_settings);

        return new GMasterCatalogImportResult(
            existingProducts.Count,
            existingCategories.Count,
            existingCartItems.Count,
            items.Count,
            categories.Count + 1,
            importedImages,
            completedUtc);
    }

    /// <summary>
    /// Collects up to <see cref="MaxPicturesPerProduct"/> photos for a product.
    /// Files follow "&lt;SKU&gt;-1.jpg", "&lt;SKU&gt;-2.jpg", ... so additional licensed photography can be
    /// dropped into the parts folder and picked up on the next import without any code change.
    /// </summary>
    private async Task<List<int>> CreateProductPicturesAsync(string imageDirectory, GMasterCatalogItem item)
    {
        var pictureIds = new List<int>(MaxPicturesPerProduct);
        if (string.IsNullOrWhiteSpace(item.ImageFile))
            return pictureIds;

        foreach (var fileName in EnumerateImageFileNames(item))
        {
            var imagePath = _fileProvider.Combine(imageDirectory, fileName);
            if (!_fileProvider.FileExists(imagePath))
                continue;

            var mimeType = _fileProvider.GetFileExtension(fileName).ToLowerInvariant() switch
            {
                ".png" => MimeTypes.ImagePng,
                ".gif" => MimeTypes.ImageGif,
                ".webp" => "image/webp",
                _ => MimeTypes.ImageJpeg
            };

            var bytes = await _fileProvider.ReadAllBytesAsync(imagePath);
            var suffix = pictureIds.Count == 0 ? string.Empty : $"-{pictureIds.Count + 1}";
            var seoName = await _pictureService.GetPictureSeNameAsync($"gmaster-{item.Sku}{suffix}");
            var picture = await _pictureService.InsertPictureAsync(
                bytes,
                mimeType,
                seoName,
                $"{item.ArabicName} - {item.EnglishName}",
                item.EnglishName,
                validateBinary: false);

            pictureIds.Add(picture.Id);
            if (pictureIds.Count == MaxPicturesPerProduct)
                break;
        }

        return pictureIds;
    }

    /// <summary>
    /// Yields the candidate file names for a product, in display order. The numeric suffix is matched
    /// exactly so a SKU that is a prefix of another SKU cannot borrow its photos.
    /// </summary>
    private static IEnumerable<string> EnumerateImageFileNames(GMasterCatalogItem item)
    {
        yield return item.ImageFile;

        string[] extensions = [".jpg", ".jpeg", ".png", ".webp"];
        for (var index = 2; index <= MaxPicturesPerProduct; index++)
        {
            foreach (var extension in extensions)
                yield return $"{item.Sku}-{index}{extension}";
        }
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

    private static Product CreateProduct(
        GMasterCatalogItem item,
        int templateId,
        int displayOrder,
        decimal costEgp,
        decimal sellingEgp)
    {
        var safeArabic = WebUtility.HtmlEncode(item.ArabicName);
        var safeEnglish = WebUtility.HtmlEncode(item.EnglishName);
        var safeOem = WebUtility.HtmlEncode(item.Oem);
        var safeModels = WebUtility.HtmlEncode(item.VehicleModels);
        var margin = sellingEgp - costEgp;
        var marginPercent = costEgp > 0 ? Math.Round(margin / costEgp * 100m, 1) : 0m;

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
            ShortDescription = $"{safeArabic} — متوافق مع {safeModels}.",
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
            AdminComment = $"Imported by GMaster. Cost RMB {item.CostRmb:0.##} → EGP {costEgp:0.##}; selling EGP {sellingEgp:0.##}; gross margin {marginPercent:0.#}%. Source {item.SourceFile}.",
            ProductTemplateId = templateId,
            AllowCustomerReviews = true,
            Price = sellingEgp,
            ProductCost = costEgp,
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
