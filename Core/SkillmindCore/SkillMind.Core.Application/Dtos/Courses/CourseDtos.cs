namespace SkillMind.Core.Application.Dtos.Courses;

public class LessonDto
{
    public Guid Id { get; set; }
    public Guid SeasonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public int DurationSeconds { get; set; }
}

public class SeasonDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<LessonDto> Lessons { get; set; } = [];
}

public class CourseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public List<SeasonDto> Seasons { get; set; } = [];
}

public class CourseCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? ProgressPercent { get; set; }
}

public class CourseProgressDto
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public Guid CourseId { get; set; }
    public Guid? LastLessonId { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime UpdatedOn { get; set; }
}

public class UpdateProgressDto
{
    public Guid LastLessonId { get; set; }
    public int ProgressPercent { get; set; }
}

public class CreateCourseDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
}

public class CreateSeasonDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class CreateLessonDto
{
    public Guid SeasonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public int DurationSeconds { get; set; }
}