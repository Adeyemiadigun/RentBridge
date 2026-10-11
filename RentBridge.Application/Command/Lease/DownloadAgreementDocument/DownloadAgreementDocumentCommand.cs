using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// A party (landlord or tenant) opens their copy of the agreement. Resolves the
/// correct file for the caller's party, records the download and returns a
/// short-lived signed download URL. For the landlord this is the lawyer's
/// source document; for the tenant it is the landlord-signed document.
/// </summary>
public sealed record DownloadAgreementDocumentCommand(Guid LeaseId) : IRequest<Result<string>>;
