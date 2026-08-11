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
    public void Parser_Should_Read_Quoted_Bilingual_Row()
    {
        const string csv = """
            sku,name_ar,name_en,oem,vehicle_models,category_key,cost_price,selling_price,source_file
            GM-51717059379,"كارتيرة أمامي شمال E90","Front fender liner, left","51717059379","E90","body-underbody",600,870,fiber.pdf
            """;

        var items = new GMasterCatalogParser().Parse(csv);
        Assert.That(items, Has.Count.EqualTo(1));

        var item = items[0];
        Action assertions = () =>
        {
            Assert.That(item.Sku, Is.EqualTo("GM-51717059379"));
            Assert.That(item.ArabicName, Does.Contain("كارتيرة"));
            Assert.That(item.EnglishName, Is.EqualTo("Front fender liner, left"));
            Assert.That(item.CostPrice, Is.EqualTo(600));
            Assert.That(item.SellingPrice, Is.EqualTo(870));
        };
        Assert.Multiple(assertions);
    }

    [Test]
    public void Parser_Should_Reject_Duplicate_Skus()
    {
        const string csv = """
            sku,name_ar,name_en,oem,vehicle_models,category_key,cost_price,selling_price,source_file
            GM-A,قطعة,Part,A,E90,body-underbody,100,160,fiber.pdf
            GM-A,قطعة أخرى,Other part,B,F30,body-underbody,100,160,fiber.pdf
            """;

        Action parseDuplicate = () => new GMasterCatalogParser().Parse(csv);
        var exception = Assert.Throws<InvalidOperationException>(parseDuplicate);
        Assert.That(exception!.Message, Does.Contain("Duplicate SKU"));
    }

    [Test]
    public void Parser_Should_Reject_Html_In_Catalog_Data()
    {
        const string csv = """
            sku,name_ar,name_en,oem,vehicle_models,category_key,cost_price,selling_price,source_file
            GM-X,قطعة,<script>alert(1)</script>,X,E90,body-underbody,100,160,fiber.pdf
            """;

        Action parseHtml = () => new GMasterCatalogParser().Parse(csv);
        var exception = Assert.Throws<InvalidOperationException>(parseHtml);

        Assert.That(exception!.Message, Does.Contain("HTML markup"));
    }

    [Test]
    public void Every_Category_Should_Have_Bilingual_Copy_And_Original_Glyph()
    {
        Assert.That(GMasterCategoryCatalog.All, Has.Count.EqualTo(10));
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
    public void Bundled_Catalog_Should_Be_Valid_Unique_And_Follow_Pricing_Policy()
    {
        var catalogPath = LocateCatalog();
        var items = new GMasterCatalogParser().Parse(File.ReadAllText(catalogPath));
        var allowedCategories = GMasterCategoryCatalog.All.Select(category => category.Key).ToHashSet();
        var pricingMismatches = items
            .Where(item => item.SellingPrice !=
                           GMasterCatalogImportService.CalculateSellingPrice(item.CostPrice))
            .Select(item =>
                $"{item.Sku}: got {item.SellingPrice}, expected {GMasterCatalogImportService.CalculateSellingPrice(item.CostPrice)}")
            .ToList();

        Action assertions = () =>
        {
            Assert.That(items, Has.Count.EqualTo(184),
                "the curated source should retain every distinct saleable line from both PDFs");
            Assert.That(items.Select(item => item.Sku), Is.Unique);
            Assert.That(items, Has.All.Matches<GMasterCatalogItem>(
                item => allowedCategories.Contains(item.CategoryKey)));
            Assert.That(pricingMismatches, Is.Empty,
                $"catalog selling prices must follow the policy: {string.Join("; ", pricingMismatches)}");
            Assert.That(items, Has.Some.Matches<GMasterCatalogItem>(
                item => item.SourceFile == "accessories.pdf"));
            Assert.That(items, Has.Some.Matches<GMasterCatalogItem>(
                item => item.SourceFile == "fiber.pdf"));
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
