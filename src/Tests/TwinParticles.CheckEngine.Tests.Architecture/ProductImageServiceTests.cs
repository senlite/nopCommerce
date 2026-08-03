using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Images;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ProductImageServiceTests
{
    [Test]
    public async Task AssignFromImportAsync_Should_Use_Placeholder_When_Source_Missing()
    {
        var service = CreateService();

        var result = await service.AssignFromImportAsync(new ImageImportRequest
        {
            ProductId = 1,
            SourceUrl = null,
            FallbackSeoName = "part-1"
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.UsedPlaceholder.Should().BeTrue();
        result.PictureId.Should().Be(999);
    }

    [Test]
    public async Task AssignFromImportAsync_Should_Quarantine_Blocked_Source()
    {
        var service = CreateService(quarantine: true);

        var result = await service.AssignFromImportAsync(new ImageImportRequest
        {
            ProductId = 2,
            SourceUrl = "http://localhost/part.png",
            FallbackSeoName = "part-2"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Quarantined.Should().BeTrue();
        result.ErrorCode.Should().Be("image.quarantined");
    }

    [Test]
    public async Task ReplacePrimaryAsync_Should_Replace_Existing_Picture_In_Place()
    {
        var repository = new FakeRepository();
        await repository.UpsertPrimaryAsync(new ProductImageRecord
        {
            ProductId = 3,
            PictureId = 777,
            QuarantineStatus = QuarantineStatus.None
        }, CancellationToken.None);

        var storage = new FakeStorage();
        var service = new ProductImageService(storage, new FakeDelivery(), new FakeQuarantine(false), repository);

        var result = await service.ReplacePrimaryAsync(3, "https://cdn.example.com/new.png", "part-3", "Alt", "عنوان", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.PictureId.Should().Be(777);
        storage.ReplacedPictureId.Should().Be(777);
    }

    private static ProductImageService CreateService(bool quarantine = false)
    {
        return new ProductImageService(new FakeStorage(), new FakeDelivery(), new FakeQuarantine(quarantine), new FakeRepository());
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

    private sealed class FakeStorage : IImageStorageService
    {
        public int? ReplacedPictureId { get; private set; }

        public Task<int?> DownloadAndCreatePictureAsync(string url, string seoName, string altText, string titleText, CancellationToken cancellationToken)
            => Task.FromResult<int?>(321);

        public Task<int?> GetDefaultPlaceholderPictureIdAsync(CancellationToken cancellationToken)
            => Task.FromResult<int?>(999);

        public Task<bool> ReplacePictureBinaryAsync(int pictureId, string sourceUrl, string seoName, string altText, string titleText, CancellationToken cancellationToken)
        {
            ReplacedPictureId = pictureId;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeDelivery : IImageDeliveryService
    {
        public Task<string?> GetVariantUrlAsync(int pictureId, ImageVariant variant, CancellationToken cancellationToken)
            => Task.FromResult<string?>($"https://cdn.test/{pictureId}/{variant.ToString().ToLowerInvariant()}");
    }

    private sealed class FakeQuarantine : IImageQuarantineService
    {
        private readonly bool _block;

        public FakeQuarantine(bool block)
        {
            _block = block;
        }

        public Task<bool> ShouldQuarantineAsync(string sourceUrl, CancellationToken cancellationToken)
            => Task.FromResult(_block);
    }
}
