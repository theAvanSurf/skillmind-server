using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Application.Dtos.Profiles;

public class UpdateProfileDto
{
    public required string ProfileName { get; set; }
    public required string ProfilePhotoUrl { get; set; }
    public required ProfileTypes ProfileType { get; set; }
    public required bool KidsProfile { get; set; }
}