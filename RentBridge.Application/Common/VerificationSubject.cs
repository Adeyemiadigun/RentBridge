namespace RentBridge.Application.Common;

/// <summary>
/// Personal details submitted with an identity-verification job so the vendor
/// can look the user up and confirm the selfie matches the ID record.
/// </summary>
public sealed record VerificationSubject(
    string GivenNames,
    string LastName,
    string Email,
    string PhoneNumber);