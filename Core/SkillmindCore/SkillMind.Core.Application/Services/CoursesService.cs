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
        progress.LastTimestampSeconds = dto.LastTimestampSeconds;
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

    public async Task<CourseDto?> PublishCourseAsync(Guid courseId, Guid professorId)
    {
        var course = await courseRepository.GetByIdAsync(courseId);
        if (course == null || course.ProfessorId != professorId)
            throw new UnauthorizedAccessException("Not authorized to publish this course.");

        course.Status = SkillMind.Core.Domain.Enums.GlobalStatus.Verified;
        await courseRepository.UpdateAsync(course.Id, course);
        return mapper.Map<CourseDto>(course);
    }

    public async Task<BrowseCoursesResultDto> BrowseCoursesAsync(
        string? search, string? category, bool? freeOnly, int page, int pageSize, string? sort)
    {
        pageSize = Math.Clamp(pageSize, 1, 40);
        page = Math.Max(0, page);

        var (courses, total) = await courseRepository.BrowseCoursesAsync(search, category, freeOnly, page, pageSize, sort);

        return new BrowseCoursesResultDto
        {
            Courses = courses.Select(MapToBrowseDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<CourseSearchSuggestionDto>> GetSuggestionsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var raw = await courseRepository.GetSuggestionsAsync(query.Trim());
        return raw.Select(r => new CourseSearchSuggestionDto
        {
            Text = r.Text,
            Type = r.Type,
            CourseId = r.CourseId
        }).ToList();
    }

    public async Task<List<string>> GetCategoriesAsync()
        => await courseRepository.GetPublishedCategoriesAsync();

    // ── Enrollment ────────────────────────────────────────────────────────────

    public async Task<CourseEnrollmentStatusDto> GetEnrollmentStatusAsync(Guid courseId, Guid profileId)
    {
        var course = await courseRepository.GetByIdAsync(courseId);
        if (course is null)
            throw new KeyNotFoundException($"Course {courseId} not found.");

        var isEnrolled = await courseRepository.IsEnrolledAsync(profileId, courseId);
        var isPaid = course.Price > 0;

        return new CourseEnrollmentStatusDto
        {
            IsEnrolled = isEnrolled,
            PurchaseRequired = isPaid && !isEnrolled,
            Price = course.Price
        };
    }

    public async Task ConfirmEnrollmentAsync(ConfirmEnrollmentDto dto)
    {
        // Idempotency: skip if already enrolled (webhook may fire twice)
        var alreadyEnrolled = await courseRepository.IsEnrolledAsync(dto.StudentProfileId, dto.CourseId);
        if (alreadyEnrolled) return;

        // Avoid duplicate from same payment intent
        var existing = await courseRepository.GetEnrollmentByPaymentIntentAsync(dto.PaymentIntentId);
        if (existing is not null) return;

        var enrollment = new SkillMind.Core.Domain.Entities.Enrollment
        {
            Id = Guid.NewGuid(),
            StudentProfileId = dto.StudentProfileId,
            CourseId = dto.CourseId,
            PaidAmount = dto.PaidAmount,
            StripePaymentIntentId = dto.PaymentIntentId,
            EnrolledAt = DateTime.UtcNow
        };

        await courseRepository.CreateEnrollmentAsync(enrollment);
    }

    public async Task<List<EnrolledCourseDto>> GetEnrolledCoursesAsync(Guid profileId)
    {
        var enrollments = await courseRepository.GetEnrolledCoursesAsync(profileId);
        return enrollments.Select(e => new EnrolledCourseDto
        {
            Id = e.Course.Id,
            Title = e.Course.Title,
            ThumbnailUrl = e.Course.ThumbnailUrl,
            Category = e.Course.Category,
            TotalSeasons = e.Course.Seasons.Count,
            TotalLessons = e.Course.Seasons.Sum(s => s.Lessons.Count),
            ProgressPercent = e.Progress?.ProgressPercent ?? 0,
            EnrolledAt = e.EnrolledAt
        }).ToList();
    }

    public async Task<CourseProgressDto?> GetProgressAsync(Guid profileId, Guid courseId)
    {
        var progress = await courseRepository.GetProgressAsync(profileId, courseId);
        return progress is null ? null : mapper.Map<CourseProgressDto>(progress);
    }

    public async Task<List<EnrolledCourseDto>> GetRecentlyWatchedAsync(Guid profileId, int limit = 20)
    {
        var records = await courseRepository.GetRecentlyWatchedByProgressAsync(profileId, limit);
        return records.Select(r =>
        {
            var allLessons = r.Course.Seasons.SelectMany(s => s.Lessons).ToList();
            var lastLesson = r.Progress.LastLessonId.HasValue
                ? allLessons.FirstOrDefault(l => l.Id == r.Progress.LastLessonId.Value)
                : null;
            return new EnrolledCourseDto
            {
                Id = r.Course.Id,
                Title = r.Course.Title,
                ThumbnailUrl = r.Course.ThumbnailUrl,
                Category = r.Course.Category,
                TotalSeasons = r.Course.Seasons.Count,
                TotalLessons = allLessons.Count,
                ProgressPercent = r.Progress.ProgressPercent,
                EnrolledAt = DateTime.UtcNow,
                LastLessonId = lastLesson?.Id,
                LastLessonTitle = lastLesson?.Title,
                LastLessonDurationSeconds = lastLesson?.DurationSeconds,
                LastTimestampSeconds = r.Progress.LastTimestampSeconds
            };
        }).ToList();
    }

    public async Task<List<EnrolledCourseDto>> GetInProgressCoursesAsync(Guid profileId)
    {
        var enrollments = await courseRepository.GetEnrolledCoursesAsync(profileId);
        return enrollments
            .Where(e => e.Progress != null && e.Progress.ProgressPercent > 0 && e.Progress.ProgressPercent < 100)
            .OrderByDescending(e => e.Progress!.UpdatedOn)
            .Select(e =>
            {
                var allLessons = e.Course.Seasons.SelectMany(s => s.Lessons).ToList();
                var lastLesson = e.Progress!.LastLessonId.HasValue
                    ? allLessons.FirstOrDefault(l => l.Id == e.Progress.LastLessonId.Value)
                    : null;

                return new EnrolledCourseDto
                {
                    Id = e.Course.Id,
                    Title = e.Course.Title,
                    ThumbnailUrl = e.Course.ThumbnailUrl,
                    Category = e.Course.Category,
                    TotalSeasons = e.Course.Seasons.Count,
                    TotalLessons = allLessons.Count,
                    ProgressPercent = e.Progress.ProgressPercent,
                    EnrolledAt = e.EnrolledAt,
                    LastLessonId = lastLesson?.Id,
                    LastLessonTitle = lastLesson?.Title,
                    LastLessonDurationSeconds = lastLesson?.DurationSeconds,
                    LastTimestampSeconds = e.Progress.LastTimestampSeconds
                };
            }).ToList();
    }

    private static BrowseCourseDto MapToBrowseDto(SkillMind.Core.Domain.Entities.Course c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Description = c.Description,
        ThumbnailUrl = c.ThumbnailUrl,
        Category = c.Category,
        Tags = c.Tags,
        Price = c.Price,
        TotalSeasons = c.Seasons.Count,
        TotalLessons = c.Seasons.Sum(s => s.Lessons.Count),
        CreatedOn = c.CreatedOn
    };
}