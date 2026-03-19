using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SkillMind.WebAPI.Extensions;

public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = $"SkillMind Core API {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                Description = "API Systems",
                Contact = new OpenApiContact
                {
                    Name = "SkillMind API Systems",
                    Email = "therealsocialhubdotnet@gmail.com"
                }
            });
        }
    }
}