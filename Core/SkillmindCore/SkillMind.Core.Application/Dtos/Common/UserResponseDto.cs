namespace SkillMind.Core.Application.Dtos.Common;

public class UserResponseDto
{
    public bool HasError { get; set; }
    public required List<string> Errors { get; set; }
}