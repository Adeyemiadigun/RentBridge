using RentBridge.Domain.Aggregates;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Renders a stored agreement document (plus certification/signature evidence)
/// into an immutable read-only PDF artifact.
/// </summary>
public interface IAgreementPdfRenderer
{
    byte[] Render(
        AgreementDocument document,
        bool isCertified,
        Guid? certifyingLawyerId,
        DateTimeOffset? certifiedAt,
        IReadOnlyCollection<SignatureRecord> signatures);
}