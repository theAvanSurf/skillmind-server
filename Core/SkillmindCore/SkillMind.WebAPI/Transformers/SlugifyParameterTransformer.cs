using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace SkillMind.WebAPI.Transformers;

public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value == null) return null;
        return Regex.Replace(value.ToString()!, "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
    }
}
