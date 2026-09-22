using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Security;
using RentBridge.Application.Dtos.Auth;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Auth
{
    internal class RefreshAccessTokenCommandHandler(
        IUnitOfWork _unitOfWork,
        ILogger<RefreshAccessTokenCommandHandler> _logger,
        IJwtTokenGenerator tokenGenerator) : IRequestHandler<RefreshAccessTokenCommand, Result<LoginResponse>>
    {
        public async Task<Result<LoginResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
        {
            var hash = TokenHasher.Hash(request.RefreshToken);

            var refreshTokenEntity = await _unitOfWork
                .Repository<RefreshToken>()
                .FirstOrDefault(t => t.TokenHash == hash, cancellationToken);

            if (refreshTokenEntity is null || !refreshTokenEntity.IsActive)
            {
                _logger.LogWarning("Refresh attempt with invalid or expired token.");
                return Result<LoginResponse>.Fail("Invalid or expired refresh token.");
            }

            var user = await _unitOfWork
                .Repository<User>()
                .FirstOrDefault(u => u.Id == refreshTokenEntity.UserId, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("Refresh attempt for missing user: {UserId}", refreshTokenEntity.UserId);
                return Result<LoginResponse>.Fail("Invalid or expired refresh token.");
            }

            var accessToken = await tokenGenerator.GenerateTokenAsync(user, cancellationToken);
            if (accessToken is null)
            {
                _logger.LogError("Token generation failed during refresh for user: {UserId}", user.Id);
                throw new InvalidOperationException("Token generation failed.");
            }

            var newRefreshToken = tokenGenerator.GenerateRefreshToken();
            var newHash = TokenHasher.Hash(newRefreshToken);

            refreshTokenEntity.Revoke(newHash);
            _unitOfWork.Repository<RefreshToken>().Update(refreshTokenEntity);
            _unitOfWork.Repository<RefreshToken>().Add(new RefreshToken(user.Id, newHash));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refresh token rotated for user: {UserId}", user.Id);
            return Result<LoginResponse>.Ok(new LoginResponse
            {
                Token = accessToken,
                RefreshToken = newRefreshToken
            });
        }
    }
}
