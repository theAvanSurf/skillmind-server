using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

public class CourseRepository(SkillMindDbContext context)
    : GenericRepository<Course>(context), ICourseRepository
{
    public async Task<Course?> GetCourseWithDetailsAsync(Guid courseId)
    {
        return await context.Courses
            .Include(c => c.Seasons.OrderBy(s => s.Order))
                .ThenInclude(s => s.Lessons.OrderBy(l => l.Order))
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public async Task<List<Course>> GetRelatedCoursesAsync(Guid courseId, string? category, int maxResults = 15)
    {
        return await context.Courses
            .Where(c => c.Id != courseId && c.Category == category)
            .OrderBy(c => c.CreatedOn)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<CourseProgress?> GetProgressAsync(Guid profileId, Guid courseId)
    {
        return await context.CourseProgresses
            .FirstOrDefaultAsync(p => p.ProfileId == profileId && p.CourseId == courseId);
    }

    public async Task<CourseProgress> UpsertProgressAsync(CourseProgress progress)
    {
        var existing = await context.CourseProgresses
            .FirstOrDefaultAsync(p => p.ProfileId == progress.ProfileId && p.CourseId == progress.CourseId);

        if (existing is null)
            await context.CourseProgresses.AddAsync(progress);
        else
        {
            existing.LastLessonId = progress.LastLessonId;
            existing.ProgressPercent = progress.ProgressPercent;
            existing.LastTimestampSeconds = progress.LastTimestampSeconds;
            existing.UpdatedOn = progress.UpdatedOn;
        }

        await context.SaveChangesAsync();
        return existing ?? progress;
    }

    public async Task<List<Course>> GetByProfessorIdAsync(Guid professorId)
    {
        return await context.Courses
            .Include(c => c.Seasons.OrderBy(s => s.Order))
                .ThenInclude(s => s.Lessons.OrderBy(l => l.Order))
            .Where(c => c.ProfessorId == professorId)
            .OrderByDescending(c => c.CreatedOn)
            .ToListAsync();
    }

    public async Task<Season> CreateSeasonAsync(Season season)
    {
        await context.Seasons.AddAsync(season);
        await context.SaveChangesAsync();
        return season;
    }

    public async Task<Lesson> CreateLessonAsync(Lesson lesson)
    {
        await context.Lessons.AddAsync(lesson);
        await context.SaveChangesAsync();
        return lesson;
    }

    public async Task<(List<Course> Courses, int TotalCount)> BrowseCoursesAsync(
        string? search, string? category, bool? freeOnly, int page, int pageSize, string? sort)
    {
        var query = context.Courses.Where(c => c.Status == GlobalStatus.Verified);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(s) ||
                c.Description.ToLower().Contains(s) ||
                (c.Category != null && c.Category.ToLower().Contains(s)) ||
                (c.Tags != null && c.Tags.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        if (freeOnly == true)
            query = query.Where(c => c.Price == 0);
        else if (freeOnly == false)
            query = query.Where(c => c.Price > 0);

        var totalCount = await query.CountAsync();

        query = sort switch
        {
            "oldest"  => query.OrderBy(c => c.CreatedOn),
            "free"    => query.OrderBy(c => c.Price).ThenByDescending(c => c.CreatedOn),
            "paid"    => query.OrderByDescending(c => c.Price).ThenByDescending(c => c.CreatedOn),
            _         => query.OrderByDescending(c => c.CreatedOn)   // "newest" default
        };

        var courses = await query
            .Include(c => c.Seasons).ThenInclude(s => s.Lessons)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (courses, totalCount);
    }

    public async Task<List<(string Text, string Type, Guid? CourseId)>> GetSuggestionsAsync(string query, int maxResults = 8)
    {
        var q = query.ToLower();
        var results = new List<(string Text, string Type, Guid? CourseId)>();

        // Course title prefix-match
        var courses = await context.Courses
            .Where(c => c.Status == GlobalStatus.Verified && c.Title.ToLower().StartsWith(q))
            .OrderBy(c => c.Title)
            .Take(5)
            .Select(c => new { c.Id, c.Title })
            .ToListAsync();

        results.AddRange(courses.Select(c => (c.Title, "course", (Guid?)c.Id)));

        // Category prefix-match
        if (results.Count < maxResults)
        {
            var cats = await context.Courses
                .Where(c => c.Status == GlobalStatus.Verified && c.Category != null && c.Category.ToLower().StartsWith(q))
                .Select(c => c.Category!)
                .Distinct()
                .Take(3)
                .ToListAsync();

            results.AddRange(cats.Select(cat => (cat, "category", (Guid?)null)));
        }

        // Tag prefix-match (stored as comma-separated strings)
        if (results.Count < maxResults)
        {
            var rawTags = await context.Courses
                .Where(c => c.Status == GlobalStatus.Verified && c.Tags != null && c.Tags.ToLower().Contains(q))
                .Select(c => c.Tags!)
                .Take(20)
                .ToListAsync();

            var matchingTags = rawTags
                .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim())
                .Where(t => t.ToLower().StartsWith(q))
                .Distinct()
                .Take(3)
                .ToList();

            results.AddRange(matchingTags.Select(tag => (tag, "tag", (Guid?)null)));
        }

        return results.Take(maxResults).ToList();
    }

    public async Task<List<string>> GetPublishedCategoriesAsync()
    {
        return await context.Courses
            .Where(c => c.Status == GlobalStatus.Verified && c.Category != null)
            .Select(c => c.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<bool> IsEnrolledAsync(Guid profileId, Guid courseId)
    {
        return await context.Enrollments
            .AnyAsync(e => e.StudentProfileId == profileId && e.CourseId == courseId);
    }

    public async Task<Enrollment> CreateEnrollmentAsync(Enrollment enrollment)
    {
        // Idempotency: return existing enrollment if already enrolled
        var existing = await context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentProfileId == enrollment.StudentProfileId && e.CourseId == enrollment.CourseId);
        if (existing is not null)
            return existing;

        await context.Enrollments.AddAsync(enrollment);
        await context.SaveChangesAsync();
        return enrollment;
    }

    public async Task<Enrollment?> GetEnrollmentByPaymentIntentAsync(string paymentIntentId)
    {
        return await context.Enrollments
            .FirstOrDefaultAsync(e => e.StripePaymentIntentId == paymentIntentId);
    }

    public async Task<Enrollment?> GetEnrollmentAsync(Guid profileId, Guid courseId)
    {
        return await context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentProfileId == profileId && e.CourseId == courseId);
    }

    public async Task MarkEnrollmentCompletedAsync(Enrollment enrollment)
    {
        context.Enrollments.Update(enrollment);
        await context.SaveChangesAsync();
    }

    public async Task<List<(Course Course, CourseProgress Progress)>> GetRecentlyWatchedByProgressAsync(Guid profileId, int limit)
    {
        var progresses = await context.CourseProgresses
            .Where(p => p.ProfileId == profileId)
            .OrderByDescending(p => p.UpdatedOn)
            .Take(limit)
            .ToListAsync();

        var courseIds = progresses.Select(p => p.CourseId).ToList();
        var courses = await context.Courses
            .Include(c => c.Seasons)
                .ThenInclude(s => s.Lessons)
            .Where(c => courseIds.Contains(c.Id))
            .ToListAsync();

        return progresses
            .Select(p => (courses.First(c => c.Id == p.CourseId), p))
            .ToList();
    }

    public async Task<List<(Course Course, CourseProgress? Progress, DateTime EnrolledAt)>> GetEnrolledCoursesAsync(Guid profileId)
    {
        var enrollments = await context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c.Seasons)
                    .ThenInclude(s => s.Lessons)
            .Where(e => e.StudentProfileId == profileId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        var courseIds = enrollments.Select(e => e.CourseId).ToList();
        var progresses = await context.CourseProgresses
            .Where(p => p.ProfileId == profileId && courseIds.Contains(p.CourseId))
            .ToListAsync();

        return enrollments.Select(e => (
            e.Course,
            progresses.FirstOrDefault(p => p.CourseId == e.CourseId),
            e.EnrolledAt
        )).ToList();
    }
}