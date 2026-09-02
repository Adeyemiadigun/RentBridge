using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Security;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Auth
{
    internal class LogoutCommandHandler(
        IUnitOfWork _unitOfWork,
        ILogger<LogoutCommandHandler> _logger) : IRequestHandler<LogoutCommand, Result>
    {
        public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var hash = TokenHasher.Hash(request.RefreshToken);

            var refreshTokenEntity = await _unitOfWork
                .Repository<RefreshToken>()
                .FirstOrDefault(t => t.TokenHash == hash, cancellationToken);

            if (refreshTokenEntity is null)
            {
                _logger.LogWarning("Logout attempt with unknown refresh token.");
                return Result.Ok();
            }

            _unitOfWork.Repository<RefreshToken>().Remove(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Refresh token deleted on logout for user: {UserId}", refreshTokenEntity.UserId);

            return Result.Ok();
        }
    }
}
