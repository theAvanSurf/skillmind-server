using AutoMapper;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class ProfilesServices(IProfilesRepository repository, IMapper mapper, IPaginationService paginationService)
    : GenericService<Profiles, ProfilesDto>(repository, mapper), IProfilesServices
{
    private readonly IMapper _mapper = mapper;

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
            .Select(dto => _mapper.Map<ProfilesDto>(dto))
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

    public async Task<PagedResultDto<ProfilesDto>> GetProfilesAsync(string userId, PagedQueryDto query)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            throw new ArgumentException("Invalid User Id");

        var search = (query as ProfilesQueryDto)?.Search;
        var sortBy = (query as ProfilesQueryDto)?.SortBy;

        var profilesQuery = repository.GetFilteredQuery(search, sortBy).Where(p => p.UserId == userGuid);
        
        var pagedProfiles = await paginationService.PaginateAsync(profilesQuery, query);
        
        var dtos = _mapper.Map<IEnumerable<ProfilesDto>>(pagedProfiles.Data);
        
        return PagedResultDto<ProfilesDto>.Create(dtos, pagedProfiles.TotalCount, pagedProfiles.Page, pagedProfiles.PageSize);
    }


    public async Task<ProfilesDto> DeleteProfile(Guid profileId, Guid userId)
    {
        var entity = await repository.GetByIdAsync(profileId);

        if (entity == null || entity.UserId != userId)
            throw new UnauthorizedAccessException("Profile not found or does not belong to the user.");

        await repository.DeleteAsync(profileId);

        return _mapper.Map<ProfilesDto>(entity);
    }

    public async Task<ProfilesDto> EditProfile(Guid profileId, Guid userId, UpdateProfileDto dto)
    {
        var entity = await repository.GetByIdAsync(profileId);

        if (entity == null || entity.UserId != userId)
            throw new UnauthorizedAccessException("Profile not found or does not belong to the user.");

        var allProfiles = await repository.GetAllAsync();

        var nameExists = allProfiles
            .Where(p => p.UserId == userId && p.Id != profileId)
            .Any(p => p.ProfileName == dto.ProfileName);
        
        entity.ProfileName = dto.ProfileName;
        entity.ProfilePhotoUrl = dto.ProfilePhotoUrl;
        entity.ProfileType = dto.ProfileType;
        entity.KidsProfile = dto.KidsProfile;

        if (nameExists)
            throw new InvalidOperationException("A profile with this name already exists.");
        
        var updatedEntity = await repository.UpdateAsync(profileId, entity);

        return updatedEntity == null ? throw new Exception("Failed to update profile.") : _mapper.Map<ProfilesDto>(updatedEntity);
    }

    public async Task<List<ProfilesDto>> GetAllProfilesAsync(Guid userId)
    {
        var profiles = await repository.GetAllAsync();
        profiles = profiles.Where(p => p.UserId == userId).ToList();
        return _mapper.Map<List<ProfilesDto>>(profiles);
    }

}