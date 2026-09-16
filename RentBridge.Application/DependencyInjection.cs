using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentBridge.Application.Common.Behaviors;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Options;
using RentBridge.Application.Services;

namespace RentBridge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ILawyerAssignmentService, LawyerAssignmentService>();
        services.AddScoped<IAgreementDocumentService, AgreementDocumentService>();
        services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();

        // Hashing:SecretKey — dev value in appsettings.development.json,
        // production via the Hashing__SecretKey environment variable.
        // Failing fast at startup avoids signing agreements with a blank key.
        var secretKey = configuration.GetSection(HashingOptions.SectionName)["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                $"{HashingOptions.SectionName}:SecretKey must be configured " +
                "(appsettings.development.json for dev, Hashing__SecretKey env var for prod).");
        }

        services.AddSingleton(new HashingOptions { SecretKey = secretKey });

        // Payment:Paystack — dev placeholders in appsettings.development.json,
        // production via Payment__Paystack__SecretKey etc. GuardValid() fails
        // fast at startup so escrow can never be funded against a blank key.
        var paymentOptions = configuration
            .GetSection(PaymentOptions.SectionName)
            .Get<PaymentOptions>() ?? new PaymentOptions();
        paymentOptions.GuardValid();

        services.AddSingleton(paymentOptions);

        return services;
    }
}
