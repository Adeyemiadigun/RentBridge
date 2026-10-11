using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// A party uploads their signed copy of the agreement (already hosted in object
/// storage). LandlordSigning or TenantSigning keeps its status; the party then
/// submits it for the lawyer's review.
/// </summary>
public sealed record UploadSignedAgreementDocumentCommand(
    Guid LeaseId,
    string DocumentUrl,
    string? FileName) : IRequest<Result>;
