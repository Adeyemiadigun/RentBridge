namespace RentBridge.Domain.Enums;

/// <summary>
/// Which stage of the document-based signing flow an uploaded file belongs to.
/// The intermediate kinds (Original, LandlordSigned) are purged once the flow
/// is finalized; the TenantSigned file (the copy bearing both signatures)
/// becomes the retained final agreement.
/// </summary>
public enum AgreementFileKind
{
    /// <summary>The source document the assigned lawyer uploads for the landlord.</summary>
    Original = 0,

    /// <summary>The landlord's signed copy, reviewed and approved by the lawyer.</summary>
    LandlordSigned,

    /// <summary>The tenant's signed copy — the final, both-signature agreement.</summary>
    TenantSigned,
}
