namespace SkillMind.Core.Application.Dtos.Common;

public class RefreshTokenRequestDto
{
    public required string UserId { get; set; }
    public required string RefreshToken { get; set; }
}
