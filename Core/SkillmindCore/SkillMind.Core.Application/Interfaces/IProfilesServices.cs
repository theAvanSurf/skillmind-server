using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Dtos.Profiles;

namespace SkillMind.Core.Application.Interfaces;

public interface IProfilesServices : IGenericService<ProfilesDto>
{
    Task<List<ProfilesDto>> SaveProfilesAsync(List<CreateProfileDto> dtos);
    Task<PagedResultDto<ProfilesDto>> GetProfilesAsync(string userId, PagedQueryDto query);
    Task<ProfilesDto> DeleteProfile(Guid profileId, Guid userId);
    Task<ProfilesDto> EditProfile(Guid profileId, Guid userId, UpdateProfileDto dto);
    Task<List<ProfilesDto>> GetAllProfilesAsync(Guid userId);
}