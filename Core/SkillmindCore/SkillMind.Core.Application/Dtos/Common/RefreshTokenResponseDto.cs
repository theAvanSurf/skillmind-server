namespace SkillMind.Core.Application.Dtos.Common;

public class RefreshTokenResponseDto
{
    public required string JwtToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public bool HasError { get; set; }
    public List<string> Errors { get; set; } = [];
}
