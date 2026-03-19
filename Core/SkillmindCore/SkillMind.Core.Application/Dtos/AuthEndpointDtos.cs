namespace SkillMind.Core.Application.Dtos.Common;

public class ValidatePasswordDto
{
    public required string UserId { get; set; }
    public required string Password { get; set; }
}

public class InvalidateSessionsDto
{
    public required string UserId { get; set; }
}
