using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchAutocompleteServiceTests
{
    [Test]
    public async Task SuggestAsync_Should_Compose_Vehicle_Oem_And_Product_Sources()
    {
        var service = SearchTestSupport.BuildAutocompleteService(
            aliases:
            [
                new VehicleAlias { NodeType = "configuration", NodeId = 501, Locale = "en", AliasText = "BMW 320i F30", NormalizedAlias = "bmw 320i f30" },
                new VehicleAlias { NodeType = "model", NodeId = 5, Locale = "en", AliasText = "BMW 3 Series", NormalizedAlias = "bmw 3 series" }
            ],
            oemPrefixMatches:
            [
                new OemNumber { Id = 9, ManufacturerId = 1, DisplayNumber = "11-51-7-586-925", NormalizedNumber = "11517586925", IsActive = true }
            ]);

        var result = await service.SuggestAsync("bmw", "en", take: 5, CancellationToken.None);

        result.Vehicles.Should().NotBeEmpty();
        result.Vehicles.Should().Contain(v => v.VehicleConfigurationId == 501, "configuration nodes carry a fitment context");
        result.Vehicles.Should().Contain(v => v.VehicleConfigurationId == null, "broader nodes are free-text vehicle suggestions");
        result.Oems.Should().ContainSingle(o => o.Value == "11-51-7-586-925");
        result.Products.Should().Contain(p => p.ProductId == 1001);
    }

    [Test]
    public async Task SuggestAsync_Should_Return_Empty_For_Short_Prefix()
    {
        var service = SearchTestSupport.BuildAutocompleteService();

        var result = await service.SuggestAsync("b", "en", take: 5, CancellationToken.None);

        result.Vehicles.Should().BeEmpty();
        result.Oems.Should().BeEmpty();
        result.Products.Should().BeEmpty();
    }

    [Test]
    public async Task SuggestAsync_Should_Cap_Each_Source_To_Requested_Take()
    {
        var service = SearchTestSupport.BuildAutocompleteService();

        var result = await service.SuggestAsync("filter", "en", take: 1, CancellationToken.None);

        result.Products.Count.Should().BeLessThanOrEqualTo(1);
    }

    [Test]
    public async Task SuggestAsync_Should_Surface_Seeded_Configuration_Alias_With_Configuration_Id()
    {
        var service = SearchTestSupport.BuildAutocompleteService(
            aliases:
            [
                new VehicleAlias
                {
                    NodeType = "configuration",
                    NodeId = 9001,
                    Locale = "en",
                    AliasText = "BMW 3 Series F30 320i ECE",
                    NormalizedAlias = "bmw 3 series f30 320i ece"
                }
            ]);

        var result = await service.SuggestAsync("F30 320i", "en", take: 5, CancellationToken.None);

        result.Vehicles.Should().ContainSingle();
        result.Vehicles[0].VehicleConfigurationId.Should().Be(9001);
        result.Vehicles[0].Label.Should().Contain("F30 320i");
    }
}
