using RentBridge.Application.Dtos.Transaction;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Dashboard;

/// <summary>One ledger total line: the summed amount and line count per type and currency.</summary>
public sealed record LedgerTypeTotal(TransactionType Type, string Currency, decimal Total, int Count);

/// <summary>Transaction metrics computed server-side from the immutable ledger.</summary>
public sealed record LedgerMetrics(IReadOnlyList<LedgerTypeTotal> Totals);

public sealed record UserSummary(
    int Total,
    int Tenants,
    int Landlords,
    int Agents,
    int Caretakers,
    int Lawyers,
    int IdentityVerified,
    int PendingLawyers);

public sealed record ListingSummary(int Total, int Draft, int Published, int Unpublished, int Closed);

public sealed record LeaseSummary(int Total, int FundedInEscrow, int Releasing, int Released);

/// <summary>Money currently held between funding and payout.</summary>
public sealed record EscrowInFlight(int LeaseCount, decimal Amount, string Currency);

/// <summary>Platform-wide operational view for admins.</summary>
public sealed record AdminDashboardResponse(
    UserSummary Users,
    ListingSummary Listings,
    LeaseSummary Leases,
    EscrowInFlight EscrowInFlight,
    LedgerMetrics Transactions);

/// <summary>Operational view for a listing owner (landlord, agent, or caretaker).</summary>
public sealed record OwnerDashboardResponse(
    ListingSummary Listings,
    LeaseSummary Leases,
    EscrowInFlight EscrowInFlight,
    LedgerMetrics Payouts,
    IReadOnlyList<TransactionItem> RecentTransactions);
