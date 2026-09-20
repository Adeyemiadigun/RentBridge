using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Metrics;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Dashboard;

public sealed class GetOwnerTransactionMetricsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetOwnerTransactionMetricsQueryHandler> logger)
    : IRequestHandler<GetOwnerTransactionMetricsQuery, Result<TransactionMetricsResponse>>
{
    public async Task<Result<TransactionMetricsResponse>> Handle(
        GetOwnerTransactionMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<TransactionMetricsResponse>.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Landlord or UserRole.Agent or UserRole.Caretaker))
        {
            logger.LogInformation("User {UserId} with role {Role} attempted to view owner transaction metrics", user.Id, user.Role);
            return Result<TransactionMetricsResponse>.Fail("Only listing owners can view these metrics.");
        }

        var points = await unitOfWork.Ledger.GetTransactionMetricsAsync(
            request.From, request.To, request.Granularity, landlordUserId: user.Id, cancellationToken);

        var series = TransactionMetricsSeries.FillGaps(points, request.From, request.To, request.Granularity);

        return Result<TransactionMetricsResponse>.Ok(
            new TransactionMetricsResponse(request.From, request.To, request.Granularity, series));
    }
}
