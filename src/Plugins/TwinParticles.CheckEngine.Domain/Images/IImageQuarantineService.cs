using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Images;

public interface IImageQuarantineService
{
    Task<bool> ShouldQuarantineAsync(string sourceUrl, CancellationToken cancellationToken);
}
