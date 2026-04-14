using AutoMapper;
using SkillMind.Core.Application.Dtos.Courses;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class CoursesService(ICourseRepository courseRepository, IMapper mapper, ICertificateService certificateService) : ICourseService
{
    public async Task<CourseDto?> GetCourseDetailsAsync(Guid courseId)
    {
        var course = await courseRepository.GetCourseWithDetailsAsync(courseId);
        return course is null ? null : mapper.Map<CourseDto>(course);
    }

    public async Task<List<CourseCardDto>> GetRelatedCoursesAsync(Guid courseId, Guid? profileId)
    {
        var course = await courseRepository.GetByIdAsync(courseId);
        var related = await courseRepository.GetRelatedCoursesAsync(courseId, course?.Category);
        var cards = mapper.Map<List<CourseCardDto>>(related);

        if (profileId.HasValue)
        {
            foreach (var card in cards)
            {
                var progress = await courseRepository.GetProgressAsync(profileId.Value, card.Id);
                card.ProgressPercent = progress?.ProgressPercent;
            }
        }
        return cards;
    }

    public async Task<CourseProgressDto?> UpdateProgressAsync(Guid profileId, Guid courseId, UpdateProgressDto dto)
    {
        var existing = await courseRepository.GetProgressAsync(profileId, courseId);

        var progress = existing ?? new CourseProgress
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            CourseId = courseId,
            ProgressPercent = 0,
            UpdatedOn = DateTime.UtcNow
        };

        progress.LastLessonId = dto.LastLessonId;
        progress.ProgressPercent = dto.ProgressPercent;
        progress.UpdatedOn = DateTime.UtcNow;

        var saved = await courseRepository.UpsertProgressAsync(progress);

        // Auto-issue certificate if threshold reached
        await certificateService.TryAutoIssueAsync(profileId, courseId, progress.ProgressPercent);

        return mapper.Map<CourseProgressDto>(saved);
    }

    public async Task<CourseDto?> CreateCourseAsync(CreateCourseDto dto)
    {
        var course = mapper.Map<Course>(dto);
        var created = await courseRepository.CreateAsync(course);
        return mapper.Map<CourseDto>(created);
    }

    public async Task<SeasonDto?> CreateSeasonAsync(CreateSeasonDto dto)
    {
        var season = mapper.Map<Season>(dto);
        var saved = await courseRepository.CreateSeasonAsync(season);
        return mapper.Map<SeasonDto>(saved);
    }

    public async Task<LessonDto?> CreateLessonAsync(CreateLessonDto dto)
    {
        var lesson = mapper.Map<Lesson>(dto);
        var saved = await courseRepository.CreateLessonAsync(lesson);
        return mapper.Map<LessonDto>(saved);
    }

    public async Task<List<CourseDto>> GetByProfessorAsync(Guid professorId)
    {
        var courses = await courseRepository.GetByProfessorIdAsync(professorId);
        return mapper.Map<List<CourseDto>>(courses);
    }

    public async Task<CourseDto?> CreateCourseForProfessorAsync(CreateCourseDto dto, Guid professorId)
    {
        var course = mapper.Map<Course>(dto);
        course.ProfessorId = professorId;
        var created = await courseRepository.CreateAsync(course);
        return mapper.Map<CourseDto>(created);
    }
}