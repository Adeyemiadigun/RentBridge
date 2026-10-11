using RentBridge.Domain.Enums;

namespace RentBridge.Api.Dtos;

/// <summary>
/// A document already hosted in object storage (uploaded via POST /uploads).
/// The URL is what gets recorded against the lease.
/// </summary>
public sealed record AgreementDocumentRequest(string Url, string? FileName = null);

/// <summary>Which party's submitted document the assigned lawyer is approving.</summary>
public sealed record ApproveAgreementDocumentRequest(LeaseParty Party);
