using Nop.Plugin.Misc.GMaster.Services;
using NUnit.Framework;

namespace Nop.Plugin.Misc.GMaster.Tests;

[TestFixture]
public class GMasterCatalogTests
{
    [TestCase(100, 160)]
    [TestCase(500, 780)]
    [TestCase(501, 730)]
    [TestCase(1_000, 1_450)]
    [TestCase(1_001, 1_360)]
    [TestCase(3_000, 4_050)]
    [TestCase(3_001, 3_850)]
    [TestCase(10_000, 12_800)]
    [TestCase(10_001, 12_210)]
    [TestCase(40_000, 48_800)]
    public void CalculateSellingPrice_Should_Apply_Tier_And_Round_Up_To_Ten(
        decimal cost,
        decimal expected)
    {
        Assert.That(GMasterCatalogImportService.CalculateSellingPrice(cost), Is.EqualTo(expected));
    }

    [Test]
    public void Parser_Should_Read_Quoted_Bilingual_Row_With_Image()
    {
        const string csv = """
            sku,name_ar,name_en,oem,vehicle_models,category_key,cost_rmb,image_file,source_file
            GM-11517586925,"طلمبة مياه كهربائية","Electric water pump, N52","11517586925","E90,E60,F10","cooling-system",360,GM-11517586925.jpeg,PILU8022228
            """;

        var items = new GMasterCatalogParser().Parse(csv);
        Assert.That(items, Has.Count.EqualTo(1));

        var item = items[0];
        Action assertions = () =>
        {
            Assert.That(item.Sku, Is.EqualTo("GM-11517586925"));
            Assert.That(item.ArabicName, Does.Contain("طلمبة"));
            Assert.That(item.EnglishName, Is.EqualTo("Electric water pump, N52"));
            Assert.That(item.CategoryKey, Is.EqualTo("cooling-system"));
            Assert.That(item.CostRmb, Is.EqualTo(360));
            Assert.That(item.ImageFile, Is.EqualTo("GM-11517586925.jpeg"));
            Assert.That(item.VehicleModels, Is.EqualTo("E90,E60,F10"));
        };
        Assert.Multiple(assertions);
    }

    [Test]
    public void Parser_Should_Reject_Duplicate_Skus()
    {
        const string csv = """
            sku,name_ar,name_en,oem,vehicle_models,category_key,cost_rmb,image_file,source_file
            GM-A,قطعة,Part,A,E90,cooling-system,100,GM-A.jpeg,PILU8022228
            GM-A,قطعة أخرى,Other part,B,F30,cooling-system,100,GM-A.jpeg,PILU8022228
            """;

        Action parseDuplicate = () => new GMasterCatalogParser().Parse(csv);
        var exception = Assert.Throws<InvalidOperationException>(parseDuplicate);
        Assert.That(exception!.Message, Does.Contain("Duplicate SKU"));
    }

    [Test]
    public void Parser_Should_Reject_Html_In_Catalog_Data()
    {
        const string csv = """
            sku,name_ar,name_en,oem,vehicle_models,category_key,cost_rmb,image_file,source_file
            GM-X,قطعة,<script>alert(1)</script>,X,E90,cooling-system,100,GM-X.jpeg,PILU8022228
            """;

        Action parseHtml = () => new GMasterCatalogParser().Parse(csv);
        var exception = Assert.Throws<InvalidOperationException>(parseHtml);

        Assert.That(exception!.Message, Does.Contain("HTML markup"));
    }

    [Test]
    public void Parser_Should_Reject_Unsafe_Image_Path()
    {
        const string csv = """
            sku,name_ar,name_en,oem,vehicle_models,category_key,cost_rmb,image_file,source_file
            GM-X,قطعة,Part,X,E90,cooling-system,100,../../secret.png,PILU8022228
            """;

        Action parsePath = () => new GMasterCatalogParser().Parse(csv);
        var exception = Assert.Throws<InvalidOperationException>(parsePath);

        Assert.That(exception!.Message, Does.Contain("Unsafe image file path"));
    }

    [Test]
    public void Every_Category_Should_Have_Bilingual_Copy_And_Original_Glyph()
    {
        Assert.That(GMasterCategoryCatalog.All, Has.Count.EqualTo(12));
        Assert.That(
            GMasterCategoryCatalog.All.Select(category => category.Key),
            Is.Unique);
        Assert.That(
            GMasterCategoryCatalog.All,
            Has.All.Matches<GMasterCategoryDefinition>(category =>
                !string.IsNullOrWhiteSpace(category.ArabicName) &&
                !string.IsNullOrWhiteSpace(category.EnglishName) &&
                !string.IsNullOrWhiteSpace(category.GlyphPath)));
    }

    [Test]
    public void ImageFactory_Should_Create_Trademark_Free_Svg()
    {
        var svg = System.Text.Encoding.UTF8.GetString(
            new GMasterImageFactory().CreateCategorySvg(GMasterCategoryCatalog.All[0]));

        Action assertions = () =>
        {
            Assert.That(svg, Does.Contain("<svg"));
            Assert.That(svg, Does.Contain("GMASTER PARTS"));
            Assert.That(svg, Does.Not.Contain("BMW"),
                "illustrations must not copy a manufacturer mark");
        };
        Assert.Multiple(assertions);
    }

    [Test]
    public void Bundled_Catalog_Should_Be_Valid_Unique_And_Photographed()
    {
        var catalogPath = LocateCatalog();
        var partsDirectory = Path.Combine(Path.GetDirectoryName(catalogPath)!, "parts");
        var items = new GMasterCatalogParser().Parse(File.ReadAllText(catalogPath));
        var allowedCategories = GMasterCategoryCatalog.All.Select(category => category.Key).ToHashSet();

        var missingImages = items
            .Where(item => !string.IsNullOrWhiteSpace(item.ImageFile))
            .Where(item => !File.Exists(Path.Combine(partsDirectory, item.ImageFile)))
            .Select(item => item.ImageFile)
            .ToList();

        var withImage = items.Count(item => !string.IsNullOrWhiteSpace(item.ImageFile));

        Action assertions = () =>
        {
            Assert.That(items, Has.Count.GreaterThanOrEqualTo(400),
                "the full container packing lists should import several hundred products");
            Assert.That(items.Select(item => item.Sku), Is.Unique);
            Assert.That(items, Has.All.Matches<GMasterCatalogItem>(
                item => allowedCategories.Contains(item.CategoryKey)));
            Assert.That(items, Has.All.Matches<GMasterCatalogItem>(item => item.CostRmb > 0));
            Assert.That(missingImages, Is.Empty,
                $"every referenced photo must be bundled: {string.Join(", ", missingImages)}");
            Assert.That(withImage, Is.GreaterThanOrEqualTo(items.Count - 5),
                "almost every product should carry a real supplier photo");
            Assert.That(items, Has.Some.Matches<GMasterCatalogItem>(
                item => item.SourceFile == "CULU6339343"));
            Assert.That(items, Has.Some.Matches<GMasterCatalogItem>(
                item => item.SourceFile == "PILU8022228"));
        };
        Assert.Multiple(assertions);
    }

    private static string LocateCatalog()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "Plugins",
                "Nop.Plugin.Misc.GMaster",
                "Content",
                "gmaster-catalog.csv");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException("Unable to locate the bundled GMaster catalog.");
    }
}
