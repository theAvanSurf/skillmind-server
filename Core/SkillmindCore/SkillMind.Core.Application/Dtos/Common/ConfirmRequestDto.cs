namespace SkillMind.Core.Application.Dtos.Common;

public class ConfirmRequestDto
{      
    public required string UserId { get; set; }
    public required string Token { get; set; }

}