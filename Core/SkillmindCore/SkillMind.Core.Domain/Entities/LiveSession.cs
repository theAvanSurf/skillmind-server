using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Domain.Entities;

public class LiveSession
{
    public required Guid Id { get; set; }
    public required Guid CourseId { get; set; }
    public required Guid ProfessorId { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    /// <summary>YouTube Broadcast ID returned by the YouTube API.</summary>
    public string? YouTubeBroadcastId { get; set; }

    /// <summary>YouTube Stream ID (the ingestion stream, not the broadcast).</summary>
    public string? YouTubeStreamId { get; set; }

    /// <summary>Public embed URL for the YouTube player.</summary>
    public string? EmbedUrl { get; set; }

    public YouTubeStreamVisibility Visibility { get; set; } = YouTubeStreamVisibility.Unlisted;
    public LiveSessionStatus Status { get; set; } = LiveSessionStatus.Scheduled;

    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public Course Course { get; set; } = null!;
    public ProfessorProfile Professor { get; set; } = null!;
}
