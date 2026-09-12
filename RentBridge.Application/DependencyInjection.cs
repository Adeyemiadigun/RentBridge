using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RentBridge.Application.Common.Behaviors;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Services;

namespace RentBridge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ILawyerAssignmentService, LawyerAssignmentService>();

        return services;
    }
}
