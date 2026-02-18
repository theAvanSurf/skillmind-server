// ProfilesMappingProfile.cs

using AutoMapper;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Application.MappingProfiles;

public class ProfilesMappingProfile : Profile
{
    public ProfilesMappingProfile()
    {
        CreateMap<CreateProfileDto, ProfilesDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id == Guid.Empty ? Guid.NewGuid() : src.Id));

        CreateMap<ProfilesDto, Profiles>().ReverseMap();
    }
}