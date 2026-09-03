using Microsoft.AspNetCore.Http;
using RentBridge.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace RentBridge.Infrastructure.Services
{
    public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        private readonly HttpContext? _httpContext = httpContextAccessor.HttpContext;
        public Guid? UserId
        {
            get
            {
                var idString = _httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier).Value;

                return Guid.TryParse(idString, out var userId) ? userId : null;
            }
        }
        public string? Email => _httpContext.User?.FindFirst(c => c.Type == ClaimTypes.Email)?.Value;
    }
}
