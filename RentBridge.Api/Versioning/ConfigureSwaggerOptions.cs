using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RentBridge.Api.Versioning;

/// <summary>
/// Registers one Swagger document per API version discovered by the versioned
/// API explorer, so each version gets its own group in the Swagger UI.
/// Also feeds the compiler XML documentation files into Swagger so action
/// summaries and DTO descriptions appear in the UI.
/// </summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = $"Rent Bridge API {description.GroupName}",
                Version = description.ApiVersion.ToString(),
                Description = "Escrowed rental marketplace API: auth, KYC (Dojah default, Smile legacy), " +
                    "property verification, listings, payout accounts, and the lease lifecycle " +
                    "(inspection → legal review → escrow → payout). " +
                    "All failures return `{ \"error\": \"message\" }`. " +
                    "Send the JWT as `Authorization: Bearer <token>`.",
            });
        }

        // Controller summaries live in RentBridge.Api; request/response shapes
        // (commands, queries, DTOs) live in the other assemblies.
        foreach (var xmlFile in new[]
        {
            "RentBridge.Api.xml",
            "RentBridge.Application.xml",
            "RentBridge.Domain.xml",
            "RentBridge.Infrastructure.xml",
        })
        {
            var path = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(path))
            {
                options.IncludeXmlComments(path, includeControllerXmlComments: true);
            }
        }
    }
}
