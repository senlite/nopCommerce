using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Images;
using TwinParticles.CheckEngine.Domain.Images;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class BatchImageReplacementServiceTests
{
    [Test]
    public async Task Batch_Should_Report_Per_Item_Outcomes_Without_One_Failure_Blocking_Others()
    {
        var lookup = new FakeLookup(new Dictionary<string, int?>
        {
            ["SKU-OK"] = 100,
            ["SKU-QUARANTINE"] = 200,
            ["SKU-MISSING"] = null
        });
        var storage = new FakeStorage();
        var repository = new FakeRepository();
        // Pre-seed existing primary images so replacement goes in place.
        await repository.UpsertPrimaryAsync(new ProductImageRecord { ProductId = 100, PictureId = 11, QuarantineStatus = QuarantineStatus.None }, CancellationToken.None);
        await repository.UpsertPrimaryAsync(new ProductImageRecord { ProductId = 200, PictureId = 22, QuarantineStatus = QuarantineStatus.None }, CancellationToken.None);

        var productImageService = new ProductImageService(storage, new FakeDelivery(), new BlocklistQuarantine("http://blocked/"), repository);
        var audit = new RecordingAudit();
        var service = new BatchImageReplacementService(productImageService, lookup, audit);

        var result = await service.ReplaceBySkuAsync(
        [
            new BatchImageReplacementItem { Sku = "SKU-OK", SourceUrl = "https://cdn.example.com/ok.png", SeoName = "ok" },
            new BatchImageReplacementItem { Sku = "SKU-QUARANTINE", SourceUrl = "http://blocked/evil.png" },
            new BatchImageReplacementItem { Sku = "SKU-MISSING", SourceUrl = "https://cdn.example.com/x.png" },
            new BatchImageReplacementItem { Sku = "", SourceUrl = "https://cdn.example.com/y.png" }
        ], "admin", CancellationToken.None);

        result.Items.Should().HaveCount(4);
        result.Replaced.Should().Be(1);
        result.Quarantined.Should().Be(1);
        result.NotFound.Should().Be(1);
        result.Failed.Should().Be(1);

        result.Items.Single(i => i.Sku == "SKU-OK").Status.Should().Be(BatchImageReplacementStatus.Replaced);
        result.Items.Single(i => i.Sku == "SKU-QUARANTINE").Status.Should().Be(BatchImageReplacementStatus.Quarantined);
        result.Items.Single(i => i.Sku == "SKU-MISSING").Status.Should().Be(BatchImageReplacementStatus.SkuNotFound);

        audit.Actions.Should().Contain("image.batch_replace");
    }

    private sealed class FakeLookup : IProductLookupService
    {
        private readonly Dictionary<string, int?> _map;
        public FakeLookup(Dictionary<string, int?> map) => _map = map;

        public Task<int?> ResolveProductIdBySkuAsync(string sku, CancellationToken cancellationToken)
            => Task.FromResult(_map.TryGetValue(sku, out var id) ? id : null);
    }

    private sealed class FakeStorage : IImageStorageService
    {
        public Task<int?> DownloadAndCreatePictureAsync(string url, string seoName, string altText, string titleText, CancellationToken cancellationToken)
            => Task.FromResult<int?>(321);
        public Task<int?> GetDefaultPlaceholderPictureIdAsync(CancellationToken cancellationToken)
            => Task.FromResult<int?>(999);
        public Task<bool> ReplacePictureBinaryAsync(int pictureId, string sourceUrl, string seoName, string altText, string titleText, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private sealed class FakeDelivery : IImageDeliveryService
    {
        public Task<string?> GetVariantUrlAsync(int pictureId, ImageVariant variant, CancellationToken cancellationToken)
            => Task.FromResult<string?>($"https://cdn.test/{pictureId}/{variant}");
    }

    private sealed class BlocklistQuarantine : IImageQuarantineService
    {
        private readonly string _blockedPrefix;
        public BlocklistQuarantine(string blockedPrefix) => _blockedPrefix = blockedPrefix;

        public Task<bool> ShouldQuarantineAsync(string sourceUrl, CancellationToken cancellationToken)
            => Task.FromResult(sourceUrl.StartsWith(_blockedPrefix, System.StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeRepository : IProductImageRepository
    {
        private readonly Dictionary<int, ProductImageRecord> _state = [];

        public Task<ProductImageRecord?> GetPrimaryAsync(int productId, CancellationToken cancellationToken)
        {
            _state.TryGetValue(productId, out var record);
            return Task.FromResult(record);
        }

        public Task UpsertPrimaryAsync(ProductImageRecord record, CancellationToken cancellationToken)
        {
            _state[record.ProductId] = record;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAudit : ICheckEngineAuditService
    {
        public List<string> Actions { get; } = [];

        public Task AppendAsync(string actor, string action, string entityType, string entityId,
            string? beforeJson, string? afterJson, CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
