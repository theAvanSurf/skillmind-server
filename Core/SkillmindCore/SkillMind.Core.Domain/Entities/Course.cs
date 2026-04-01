using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Domain.Entities;

public class Course
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public required GlobalStatus Status { get; set; }
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public ICollection<Season> Seasons { get; set; } = [];
}