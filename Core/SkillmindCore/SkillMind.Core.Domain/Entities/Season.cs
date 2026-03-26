namespace SkillMind.Core.Domain.Entities;

public class Season
{
    public required Guid Id { get; set; }
    public required Guid CourseId { get; set; }
    public required string Title { get; set; }
    public required int Order { get; set; }
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = [];
}