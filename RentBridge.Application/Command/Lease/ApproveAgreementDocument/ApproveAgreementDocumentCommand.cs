using MediatR;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// The assigned lawyer approves a party's submitted signed document. Approving
/// the landlord opens the tenant's signing step (→ TenantSigning); approving the
/// tenant fully signs the agreement (→ FullySigned) and unlocks escrow funding.
/// </summary>
public sealed record ApproveAgreementDocumentCommand(Guid LeaseId, LeaseParty Party) : IRequest<Result>;
