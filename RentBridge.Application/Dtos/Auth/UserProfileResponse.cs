namespace RentBridge.Application.Dtos.Auth;

/// <summary>
/// Current user's profile returned by GET /auth/me. Status is a serialized
/// role name and verified reflects identity verification (KYC).
/// </summary>
public sealed record UserProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Name,
    string Email,
    string Phone,
    string Role,
    bool IdentityVerified);