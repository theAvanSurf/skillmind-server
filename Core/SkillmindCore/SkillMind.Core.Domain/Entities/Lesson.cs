namespace SkillMind.Core.Domain.Entities;

public class Lesson
{
    public required Guid Id { get; set; }
    public required Guid SeasonId { get; set; }
    public required string Title { get; set; }
    public required string VideoUrl { get; set; }
    public string? Description { get; set; }
    public required int Order { get; set; }
    public int DurationSeconds { get; set; }
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public Season Season { get; set; } = null!;
}