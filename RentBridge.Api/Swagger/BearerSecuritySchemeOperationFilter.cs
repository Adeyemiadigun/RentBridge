using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RentBridge.Api.Swagger;

/// <summary>
/// Shows the Bearer lock icon only on operations that actually require
/// authentication. Anonymous endpoints (register, login, public search,
/// provider webhooks) carry no security requirement in the UI.
/// </summary>
public sealed class BearerSecuritySchemeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodAllowsAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>().Any();
        var controllerAllowsAnonymous = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() is true;

        var methodRequiresAuth = context.MethodInfo.GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>().Any();
        var controllerRequiresAuth = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() is true;

        // Explicit [AllowAnonymous] always wins (e.g. public search inside a
        // mixed controller, webhook ingress).
        if (methodAllowsAnonymous || (!methodRequiresAuth && !controllerRequiresAuth))
        {
            return;
        }

        if (controllerAllowsAnonymous && !methodRequiresAuth)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", null, null),
                []
            },
        });
    }
}
