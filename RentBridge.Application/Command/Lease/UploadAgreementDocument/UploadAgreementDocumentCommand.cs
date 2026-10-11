using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// The assigned lawyer uploads the agreement document (already hosted in object
/// storage) for the landlord to download and sign. LegalReview → LandlordSigning.
/// </summary>
public sealed record UploadAgreementDocumentCommand(
    Guid LeaseId,
    string DocumentUrl,
    string? FileName) : IRequest<Result>;
