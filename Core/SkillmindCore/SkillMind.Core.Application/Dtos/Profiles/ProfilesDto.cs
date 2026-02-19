using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Application.Dtos.Profiles;

public class ProfilesDto
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string ProfileName { get; set; }
    public required string ProfilePhotoUrl { get; set; }
    public required ProfileTypes ProfileType { get; set; }
    public required bool KidsProfile { get; set; }
}