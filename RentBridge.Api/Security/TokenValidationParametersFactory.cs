using System.Text;
using Microsoft.IdentityModel.Tokens;
using RentBridge.Infrastructure.Services;

namespace RentBridge.Api.Security;

/// <summary>
/// Single source of truth for JWT validation parameters. The bearer setup in
/// Program.cs and the Hangfire dashboard filter both derive from this so the
/// rules (issuer, audience, lifetime, signing key) can never drift apart.
/// </summary>
public static class TokenValidationParametersFactory
{
    public static TokenValidationParameters Create(JwtOptions jwtOptions) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
    };
}
