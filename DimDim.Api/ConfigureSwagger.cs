using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace DimDim.Api;
public class ConfigureSwagger(IApiVersionDescriptionProvider versions) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var version in versions.ApiVersionDescriptions) options.SwaggerDoc(version.GroupName, new OpenApiInfo {
            Title = "DimDim " + version.ApiVersion, Version = version.ApiVersion.ToString(),
            Description = version.IsDeprecated ? "DEPRECADA: lista antiga. Prefira v2.0 com paginação." : "Versão atual: listagem de transações paginada."
        });
    }
}
