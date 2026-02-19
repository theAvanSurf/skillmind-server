using System.Text.Json.Serialization;
using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Application.Dtos.Profiles;

public class CreateProfileDto
{
    [JsonIgnore] 
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonIgnore]
    public Guid UserId { get; set; }
    public required string ProfileName { get; set; }
    public required string ProfilePhotoUrl { get; set; }
    public required ProfileTypes ProfileType { get; set; }
    public required bool KidsProfile { get; set; }
}