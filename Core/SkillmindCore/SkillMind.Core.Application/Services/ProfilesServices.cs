using AutoMapper;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class ProfilesServices(
    IProfilesRepository repository,
    IMapper mapper,
    IPaginationService paginationService,
    ISessionManager sessionManager)
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
        
        List<ProfilesDto> savedProfiles;

        switch (newDtos.Count)
        {
            case 0:
                return [];
            case 1:
            {
                var result = await AddAsync(newDtos[0]);
                if (result == null) throw new Exception($"Failed to create profile '{newDtos[0].ProfileName}'.");
                savedProfiles = [result];
                break;
            }
            default:
                savedProfiles = await AddRangeAsync(newDtos) ?? throw new Exception("Failed to create profiles.");
                break;
        }

        // Update session for each new profile
        if (savedProfiles.Count > 0)
        {
            // Assuming all profiles belong to the same user (which they should based on Controller logic)
            var userId = savedProfiles.First().UserId;
            foreach (var profile in savedProfiles)
            {
                await sessionManager.AddProfileAsync(userId, profile);
            }
        }

        return savedProfiles;
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

        // Remove from session
        await sessionManager.RemoveProfileAsync(userId, profileId);

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
        var resultDto = updatedEntity == null ? throw new Exception("Failed to update profile.") : _mapper.Map<ProfilesDto>(updatedEntity);

        // Update in session (Remove old, Add new/updated)
        // Alternatively we could have an UpdateProfileAsync on SessionManager, 
        // but Remove+Add is a safe way to ensure it's refreshed.
        await sessionManager.RemoveProfileAsync(userId, profileId);
        await sessionManager.AddProfileAsync(userId, resultDto);

        return resultDto;
    }

    public async Task<List<ProfilesDto>> GetAllProfilesAsync(Guid userId)
    {
        var profiles = await repository.GetAllAsync();
        profiles = profiles.Where(p => p.UserId == userId).ToList();
        return _mapper.Map<List<ProfilesDto>>(profiles);
    }

}