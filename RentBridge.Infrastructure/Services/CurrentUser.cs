using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace RentBridge.Infrastructure.Services
{
    public class CurrentUser(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUser> logger,IUnitOfWork unitOfWork) : ICurrentUser
    {
        private readonly HttpContext? _httpContext = httpContextAccessor.HttpContext;
        public Guid? UserId
        {
            get
            {
                var idString = _httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return Guid.TryParse(idString, out var userId) ? userId : null;
            }
        }
        public string? Email => _httpContext.User?.FindFirst(c => c.Type == ClaimTypes.Email)?.Value;

        public async Task<Result<User>> GetCurrentUser(bool isIdentityVerified = false, CancellationToken cancellationToken = default)
        {
            var userId = UserId;
            if (userId is null)
            {
                logger.LogInformation("User not authenticated");
                return Result<User>.Fail("User not Authenticated");
            }

            var user = await unitOfWork.Repository<User>().FirstOrDefault(u => u.Id == UserId, cancellationToken);
            if (user == null)
            {
                logger.LogInformation("User not found");
                return Result<User>.Fail("User not found");
            }
            if (!user.IdentityVerified && isIdentityVerified)
            {
                logger.LogInformation("User identity not verified");
                return Result<User>.Fail("User identity not verified");
            }
            return Result<User>.Ok(user);

        }
    }
}
