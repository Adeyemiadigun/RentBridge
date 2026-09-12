using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RentBridge.Api.Hangfire;

/// <summary>
/// Exposes the Hangfire dashboard only while running in Development.
/// Production deployments must swap this for real identity-based authorization.
/// </summary>
public sealed class DevelopmentOnlyDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var environment = httpContext?.RequestServices?.GetService<IHostEnvironment>();
        return environment?.IsDevelopment() ?? false;
    }
}