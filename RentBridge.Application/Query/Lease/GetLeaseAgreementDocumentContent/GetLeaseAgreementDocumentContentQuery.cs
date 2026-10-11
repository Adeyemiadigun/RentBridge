using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Lease;

/// <summary>
/// Returns the bytes of the agreement document the caller is allowed to see,
/// proxied so the API controls the content disposition (inline = view in the
/// browser, attachment = download). Cloudinary serves raw files as attachments
/// regardless of the link, so a direct storage URL always forces a download.
/// </summary>
public sealed record GetLeaseAgreementDocumentContentQuery(Guid LeaseId)
    : IRequest<Result<AgreementDocumentContent>>;

public sealed record AgreementDocumentContent(byte[] Content, string ContentType, string? FileName);
