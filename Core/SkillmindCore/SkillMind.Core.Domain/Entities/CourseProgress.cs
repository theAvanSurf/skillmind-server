namespace SkillMind.Core.Domain.Entities;

public class CourseProgress
{
    public required Guid Id { get; set; }
    public required Guid ProfileId { get; set; }
    public required Guid CourseId { get; set; }
    public Guid? LastLessonId { get; set; }
    public required int ProgressPercent { get; set; } = 0;
    public required DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
}