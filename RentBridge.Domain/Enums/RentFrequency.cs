namespace RentBridge.Domain.Enums;

/// <summary>
/// How often the rent amount is due. Used for display and proration.
/// </summary>
public enum RentFrequency
{
    Monthly = 1,
    Quarterly = 3,       // every 3 months
    SemiAnnually = 6,    // every 6 months
    Annually = 12,       // every 12 months
    Custom = 99          // fallback for bespoke periods (store months in separate field if needed)
}