namespace SkillMind.Core.Application.Dtos.Common;

public class JwtResponseDto
{
    public bool? HasError { get; set; }
    public string? Error { get; set; }
}