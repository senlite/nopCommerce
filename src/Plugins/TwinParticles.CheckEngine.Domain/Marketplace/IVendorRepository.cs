using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IVendorRepository
{
    Task<int> InsertAsync(Vendor vendor, CancellationToken cancellationToken);

    Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken);

    Task<Vendor?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Vendor>> GetByStatusesAsync(IReadOnlyCollection<VendorStatus> statuses, CancellationToken cancellationToken);

    Task<Vendor?> GetOperatorAsync(CancellationToken cancellationToken);

    Task<Vendor?> GetByApplicantCustomerIdAsync(int customerId, CancellationToken cancellationToken);

    Task InsertAgreementAsync(VendorAgreementAcceptance acceptance, CancellationToken cancellationToken);

    Task<VendorAgreementAcceptance?> GetLatestAgreementAsync(int vendorId, CancellationToken cancellationToken);

    Task<bool> HasAcceptedAgreementAsync(int vendorId, string agreementVersion, CancellationToken cancellationToken);

    Task<string?> GetApplicantAccessTokenHashAsync(int vendorId, CancellationToken cancellationToken);
}
