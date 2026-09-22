using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
namespace RentBridge.Application.Events.InspectionConfirmed;
using InspectionConfirmedEvent = RentBridge.Domain.Aggregates.InspectionConfirmed;
public sealed class AutoAssignLeaseLawyerOnInspectionConfirmedEventHandler(
    IUnitOfWork unitOfWork,
    ILawyerAssignmentService lawyerAssignmentService,
    ILogger<AutoAssignLeaseLawyerOnInspectionConfirmedEventHandler> logger)
    : INotificationHandler<InspectionConfirmedEvent>
{
    public async Task Handle(InspectionConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("Cannot auto-assign lease lawyer: lease {LeaseId} not found.", notification.LeaseId);
            return;
        }

        var lawyer = await lawyerAssignmentService.PickNextVerifiedLawyerAsync(cancellationToken);
        if (!lawyer.IsSuccess)
        {
            logger.LogWarning(
                "Cannot auto-assign lease lawyer for lease {LeaseId}: no verified lawyer available ({Error}).",
                lease.Id, lawyer.Error);
            return;
        }

        var result = lease.AssignLawyer(lawyer.Value);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Auto-assigning lawyer {LawyerId} to lease {LeaseId} failed: {Error}", lawyer.Value, lease.Id, result.Error);
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
