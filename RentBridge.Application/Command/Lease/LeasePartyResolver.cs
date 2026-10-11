using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Maps the calling user to the lease party they act as. Kept in one place so
/// "who is this party" cannot drift between the document-flow handlers.
/// </summary>
internal static class LeasePartyResolver
{
    public static bool TryResolve(Guid userId, LeaseAggregate lease, out LeaseParty party)
    {
        if (userId == lease.TenantUserId)
        {
            party = LeaseParty.Tenant;
            return true;
        }

        if (userId == lease.LandlordUserId)
        {
            party = LeaseParty.Landlord;
            return true;
        }

        party = default;
        return false;
    }
}
