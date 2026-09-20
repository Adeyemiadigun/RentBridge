using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Metrics;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Admin;

public sealed class GetTransactionMetricsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetTransactionMetricsQueryHandler> logger)
    : IRequestHandler<GetTransactionMetricsQuery, Result<TransactionMetricsResponse>>
{
    public async Task<Result<TransactionMetricsResponse>> Handle(
        GetTransactionMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<TransactionMetricsResponse>.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to view transaction metrics without admin role", actor.Id);
            return Result<TransactionMetricsResponse>.Fail("Only an admin can view transaction metrics.");
        }

        var points = await unitOfWork.Ledger.GetTransactionMetricsAsync(
            request.From, request.To, request.Granularity, landlordUserId: null, cancellationToken);

        var series = TransactionMetricsSeries.FillGaps(points, request.From, request.To, request.Granularity);

        return Result<TransactionMetricsResponse>.Ok(
            new TransactionMetricsResponse(request.From, request.To, request.Granularity, series));
    }
}
