using AutoMapper;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class ProfilesServices(IProfilesRepository repository, IMapper mapper)
    : GenericService<Profiles, ProfilesDto>(repository, mapper), IProfilesServices
{
    public async Task<List<ProfilesDto>> SaveProfilesAsync(List<CreateProfileDto> dtos)
    {
        var existingProfiles = await GetAll();

        var duplicates = dtos
            .Where(dto => existingProfiles.Any(p => p.ProfileName == dto.ProfileName))
            .Select(dto => dto.ProfileName)
            .ToList();

        if (duplicates.Count != 0)
            throw new InvalidOperationException($"The following profiles already exist: {string.Join(", ", duplicates)}");

        var newDtos = dtos
            .Where(dto => existingProfiles.All(p => p.ProfileName != dto.ProfileName))
            .Select(dto => mapper.Map<ProfilesDto>(dto))
            .ToList();

        switch (newDtos.Count)
        {
            case 0:
                return [];
            case 1:
            {
                var result = await AddAsync(newDtos[0]);
                if (result == null) throw new Exception($"Failed to create profile '{newDtos[0].ProfileName}'.");
                return [result];
            }
            default:
                return await AddRangeAsync(newDtos) ?? throw new Exception("Failed to create profiles.");
        }
    }
}