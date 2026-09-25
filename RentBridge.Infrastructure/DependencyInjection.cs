using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Interfaces.Verification;
using RentBridge.Application.Common.Options;
using RentBridge.Application.Common.Payments;
using RentBridge.Infrastructure.Persistence;
using RentBridge.Infrastructure.Persistence.Repositories;
using RentBridge.Infrastructure.Services;
using RentBridge.Infrastructure.Verification;
using RentBridge.Infrastructure.Verification.Providers.Dojah;
using RentBridge.Infrastructure.Verification.Providers.Smile;

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
        services.AddSingleton<IDojahSignatureValidator, DojahSignatureValidator>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordService,PasswordService>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IBackgroundJobDispatcher, HangfireJobDispatcher>();
        services.AddHttpClient<IEmailSender, BrevoEmailService>();
        services.AddScoped<IEmailService, BackgroundEmailService>();

        // Identity-verification strategies. Default is Verification:Provider
        // ("dojah"); add new vendors as IIdentityVerificationProvider and
        // point the config at their ProviderName — no caller changes needed.
        services.Configure<VerificationOptions>(
            configuration.GetSection(VerificationOptions.SectionName));
        services.Configure<DojahOptions>(
            configuration.GetSection(DojahOptions.SectionName));
        services.Configure<CloudinaryOptions>(
            configuration.GetSection(CloudinaryOptions.SectionName));
        services.AddHttpClient<IFileStorage, CloudinaryFileStorage>();
        services.AddScoped<DojahIdentityVerificationProvider>();
        services.AddHttpClient<SmileIdentityVerificationProvider>();
        services.AddScoped<IIdentityVerificationProvider>(
            sp => sp.GetRequiredService<DojahIdentityVerificationProvider>());
        services.AddScoped<IIdentityVerificationProvider>(
            sp => sp.GetRequiredService<SmileIdentityVerificationProvider>());
        services.AddScoped<IIdentityVerificationProviderFactory, VerificationProviderFactory>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAgreementPdfRenderer, AgreementPdfRenderer>();

        services.AddHttpClient<IEscrowProvider, PaystackEscrowProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
            client.BaseAddress = new Uri(options.Paystack.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Paystack.SecretKey);
        });

        return services;
    }
}
