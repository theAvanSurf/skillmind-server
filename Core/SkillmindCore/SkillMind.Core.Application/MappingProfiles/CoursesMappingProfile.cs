using AutoMapper;
using SkillMind.Core.Application.Dtos.Courses;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Application.MappingProfiles;

public class CoursesMappingProfile : Profile
{
    public CoursesMappingProfile()
    {
        CreateMap<Course, CourseDto>().ReverseMap();
        CreateMap<Course, CourseCardDto>();
        CreateMap<Season, SeasonDto>().ReverseMap();
        CreateMap<Lesson, LessonDto>().ReverseMap();
        CreateMap<CourseProgress, CourseProgressDto>().ReverseMap();
        CreateMap<CreateCourseDto, Course>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => SkillMind.Core.Domain.Enums.GlobalStatus.Active))
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(_ => DateTime.UtcNow));
        CreateMap<CreateSeasonDto, Season>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(_ => DateTime.UtcNow));
        CreateMap<CreateLessonDto, Lesson>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}