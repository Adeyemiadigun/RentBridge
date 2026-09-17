using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RentBridge.Api.Versioning;

/// <summary>
/// Registers one Swagger document per API version discovered by the versioned
/// API explorer, so each version gets its own group in the Swagger UI.
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
                Description = "Escrowed rental marketplace API: KYC, property verification, listings, and the lease lifecycle."
            });
        }
    }
}
