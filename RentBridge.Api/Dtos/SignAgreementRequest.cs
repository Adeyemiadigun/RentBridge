namespace RentBridge.Api.Dtos;

/// <summary>
/// Optional payload for signing an agreement. SignatureImage is a base64-encoded
/// image of the drawn signature; when omitted a deterministic hash is stored.
/// </summary>
public sealed record SignAgreementRequest(string? SignatureImage = null);