using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Payments;
using RentBridge.Infrastructure.Persistence;
using RentBridge.Infrastructure.Persistence.Repositories;
using RentBridge.Infrastructure.Services;
using RentBridge.Infrastructure.Verification;

namespace RentBridge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<ISmileSignatureValidator, SmileSignatureValidator>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordService,PasswordService>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IBackgroundJobDispatcher, HangfireJobDispatcher>();
        services.AddHttpClient<IEmailSender, BrevoEmailService>();
        services.AddScoped<IEmailService, BackgroundEmailService>();
        services.AddHttpClient<IIdentityVerificationService, SmileIdentityService>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAgreementPdfRenderer, AgreementPdfRenderer>();

        services.AddHttpClient<IEscrowProvider, PaystackEscrowProvider>();

        return services;
    }
}
