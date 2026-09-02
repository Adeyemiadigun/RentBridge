namespace RentBridge.Infrastructure.Verification;

/// <summary>Verifies that an incoming webhook genuinely came from Dojah.</summary>
public interface IDojahSignatureValidator
{
    bool IsValid(string rawBody, string signature);
}
