using RentBridge.Domain.Enums;

namespace RentBridge.Domain.ValueObjects
{
    public sealed record SignatureRecord(LeaseParty Party, string ImageHash, DateTimeOffset SignedAt, string IpAddress);
}
