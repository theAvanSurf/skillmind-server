using SkillMind.Core.Application.Dtos.Profiles;

namespace SkillMind.Core.Application.Interfaces;

public interface IProfilesServices : IGenericService<ProfilesDto>
{
    Task<List<ProfilesDto>> SaveProfilesAsync(List<CreateProfileDto> dtos);
}