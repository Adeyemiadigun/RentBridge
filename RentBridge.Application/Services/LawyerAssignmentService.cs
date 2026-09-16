using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Services;

/// <summary>
/// Round-robin auto-assignment of verified lawyers to property verification.
/// Never-assigned lawyers are picked first (Least Recently Assigned); when the
/// assigned lawyer becomes unavailable (suspended/rejected), the reviewer is
/// re-picked lazily at review time. An admin can always override manually.
/// </summary>
public class LawyerAssignmentService(
    IUnitOfWork unitOfWork,
    ILogger<LawyerAssignmentService> logger) : ILawyerAssignmentService
{
    public async Task<Result<Guid>> PickNextVerifiedLawyerAsync(CancellationToken ct)
    {
        var page = await unitOfWork.Repository<User>()
            .GetPagedAsync(
                u => u.Role == UserRole.Lawyer
                     && u.LawyerProfile != null
                     && u.LawyerProfile.Status == LawyerStatus.Verified,
                1,
                100,
                u => u.LawyerProfile!.LastAssignedAt ?? DateTimeOffset.MinValue,
                true,
                ct);

        var nominee = page.Items.FirstOrDefault();
        if (nominee is null)
            return Result<Guid>.Fail("No verified lawyer is currently available.");

        var tracked = await unitOfWork.Repository<User>().GetByIdAsync(nominee.Id, ct);
        tracked?.LawyerProfile?.MarkAssigned();

        return Result<Guid>.Ok(nominee.Id);
    }

    public async Task<Result<Guid>> ResolveAndAuthorizeAsync(PropertyAggregate property, User actor, CancellationToken ct)
    {
        var current = property.VerificationLawyerId;

        if (current is null || !await IsAssignedLawyerVerifiedAsync(current.Value, ct))
        {
            var picked = await PickNextVerifiedLawyerAsync(ct);
            if (!picked.IsSuccess)
                return Result<Guid>.Fail("No verified lawyer is currently available. Contact an admin.");

            if (current is not null)
                logger.LogWarning(
                    "Reassigning verification lawyer for property {PropertyId}: {Previous} -> {Next}",
                    property.Id, current.Value, picked.Value);

            var assignResult = property.AssignVerificationLawyer(picked.Value);
            if (!assignResult.IsSuccess)
                return Result<Guid>.Fail(assignResult.Error!);

            current = picked.Value;
        }

        if (actor.Role == UserRole.Admin)
            return Result<Guid>.Ok(current.Value);

        if (actor.Role != UserRole.Lawyer)
            return Result<Guid>.Fail("Only a lawyer or admin can review property documents.");

        if (actor.Id != current.Value)
            return Result<Guid>.Fail("Only the assigned lawyer can review this property's documents.");

        return Result<Guid>.Ok(current.Value);
    }


  private async Task<bool> IsAssignedLawyerVerifiedAsync(Guid lawyerId, CancellationToken ct)
    {
        var lawyer = await unitOfWork.Repository<User>().FirstOrDefault(u => u.Id == lawyerId, ct);
        return lawyer is { Role: UserRole.Lawyer }
               && lawyer.LawyerProfile is { Status: LawyerStatus.Verified };
    }
}