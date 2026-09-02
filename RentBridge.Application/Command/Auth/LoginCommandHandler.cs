using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Auth;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Auth
{
    internal class LoginCommandHandler(IUnitOfWork _unitOfWork, ILogger<LoginCommandHandler> _logger,IJwtTokenGenerator tokenGenerator,IPasswordService passwordService) : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Repository<Domain.Aggregates.Users.User>().FirstOrDefault(x => x.Email == request.Email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed for email: {Email}", request.Email);
                return Result<LoginResponse>.Fail("Invalid credentials.");
            }

            if (!passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Login attempt failed for email: {Email} due to incorrect password.", request.Email);
                return Result<LoginResponse>.Fail("Invalid credentials.");
            }

            var token = await tokenGenerator.GenerateTokenAsync(user, cancellationToken);
            var refreshToken = tokenGenerator.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken(user.Id, refreshToken);

            if (token == null)
            {
                _logger.LogError("Token generation failed for user: {UserId}", user.Id);
                throw new InvalidOperationException("Token generation failed.");
            }

             _unitOfWork.Repository<RefreshToken>().Add(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LoginResponse>.Ok(new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken
            });


        }
    }
}
