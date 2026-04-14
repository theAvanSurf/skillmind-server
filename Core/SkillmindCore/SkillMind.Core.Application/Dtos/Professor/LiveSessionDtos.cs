namespace SkillMind.Core.Application.Dtos.Professor;

// ─── Live Streaming ───────────────────────────────────────────────────────────

public class CreateLiveSessionDto
{
    public Guid CourseId { get; set; }
    public Guid ProfessorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = "Unlisted";
    public DateTime? ScheduledAt { get; set; }
}

public class LiveSessionDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EmbedUrl { get; set; }
    public string? YouTubeBroadcastId { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class LiveSessionCreatedDto : LiveSessionDto
{
    /// <summary>RTMP stream key for OBS/encoder. Only returned at creation — not stored in the DB.</summary>
    public string? StreamKey { get; set; }
    public string? RtmpIngestUrl { get; set; }
}

public class YouTubeOAuthUrlDto
{
    public string AuthorizationUrl { get; set; } = string.Empty;
}

public class ExchangeOAuthCodeDto
{
    public required string Code { get; set; }
}

public class YouTubeStatusDto
{
    public bool IsConnected { get; set; }
}
