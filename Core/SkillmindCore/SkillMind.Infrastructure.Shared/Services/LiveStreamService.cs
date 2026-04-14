using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Infrastructure.Shared.Services;

public class LiveStreamService(
    IProfessorRepository professorRepository,
    ILiveSessionRepository liveSessionRepository,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ILiveStreamService
{
    private const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";
    private const string YouTubeApiBase = "https://www.googleapis.com/youtube/v3";

    private string ClientId => configuration["Google:ClientId"]
        ?? throw new InvalidOperationException("Google:ClientId is not configured.");

    private string ClientSecret => configuration["Google:ClientSecret"]
        ?? throw new InvalidOperationException("Google:ClientSecret is not configured.");

    private string RedirectUri => configuration["Google:RedirectUri"]
        ?? throw new InvalidOperationException("Google:RedirectUri is not configured.");

    // ── OAuth ─────────────────────────────────────────────────────────────────

    public string GetAuthorizationUrl(Guid professorId)
    {
        const string scopes = "https://www.googleapis.com/auth/youtube.force-ssl";
        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(professorId.ToString()));

        return "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task ExchangeCodeAsync(Guid professorId, string code)
    {
        var http = httpClientFactory.CreateClient();
        var response = await http.PostAsync(GoogleTokenUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["redirect_uri"] = RedirectUri,
                ["grant_type"] = "authorization_code"
            }));

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<GoogleTokenResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse Google token response.");

        var professor = await professorRepository.GetByIdAsync(professorId)
            ?? throw new KeyNotFoundException("Professor profile not found.");

        professor.YouTubeAccessToken = token.AccessToken;
        if (!string.IsNullOrEmpty(token.RefreshToken))
            professor.YouTubeRefreshToken = token.RefreshToken;
        professor.YouTubeTokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 60);
        professor.UpdatedOn = DateTime.UtcNow;

        await professorRepository.UpdateAsync(professor);
    }

    // ── Broadcasts ────────────────────────────────────────────────────────────

    public async Task<LiveSessionCreatedDto> CreateBroadcastAsync(CreateLiveSessionDto dto)
    {
        var professor = await professorRepository.GetByIdAsync(dto.ProfessorId)
            ?? throw new KeyNotFoundException("Professor profile not found.");

        var accessToken = await GetValidAccessTokenAsync(professor);
        var http = CreateYouTubeClient(accessToken);

        var visibility = MapVisibility(dto.Visibility);
        var scheduledAt = dto.ScheduledAt ?? DateTime.UtcNow.AddMinutes(5);

        // 1. Create broadcast
        var broadcastId = await CreateYouTubeBroadcastAsync(http, dto.Title, dto.Description, visibility, scheduledAt);

        // 2. Create ingestion stream
        var (streamId, streamKey, rtmpUrl) = await CreateYouTubeStreamAsync(http, dto.Title);

        // 3. Bind broadcast to stream
        await BindBroadcastToStreamAsync(http, broadcastId, streamId);

        var session = new LiveSession
        {
            Id = Guid.NewGuid(),
            CourseId = dto.CourseId,
            ProfessorId = dto.ProfessorId,
            Title = dto.Title,
            Description = dto.Description,
            YouTubeBroadcastId = broadcastId,
            YouTubeStreamId = streamId,
            EmbedUrl = $"https://www.youtube.com/embed/{broadcastId}",
            Visibility = MapVisibilityEnum(dto.Visibility),
            Status = LiveSessionStatus.Scheduled,
            ScheduledAt = dto.ScheduledAt,
            CreatedOn = DateTime.UtcNow
        };

        var created = await liveSessionRepository.CreateAsync(session);

        return new LiveSessionCreatedDto
        {
            Id = created.Id,
            CourseId = created.CourseId,
            CourseTitle = string.Empty,
            Title = created.Title,
            Description = created.Description,
            EmbedUrl = created.EmbedUrl,
            YouTubeBroadcastId = created.YouTubeBroadcastId,
            Visibility = created.Visibility.ToString(),
            Status = created.Status.ToString(),
            ScheduledAt = created.ScheduledAt,
            CreatedOn = created.CreatedOn,
            StreamKey = streamKey,
            RtmpIngestUrl = rtmpUrl
        };
    }

    public async Task<LiveSessionDto> StartBroadcastAsync(Guid sessionId)
    {
        var session = await liveSessionRepository.GetByIdAsync(sessionId)
            ?? throw new KeyNotFoundException($"Live session {sessionId} not found.");

        // enableAutoStart=true on the broadcast means YouTube transitions to "live"
        // automatically when OBS starts streaming — no manual transition needed here.
        // Go Live just marks our local status.

        session.Status = LiveSessionStatus.Live;
        session.StartedAt = DateTime.UtcNow;
        return MapToDto(await liveSessionRepository.UpdateAsync(session));
    }

    public async Task<LiveSessionDto> EndBroadcastAsync(Guid sessionId)
    {
        var session = await liveSessionRepository.GetByIdAsync(sessionId)
            ?? throw new KeyNotFoundException($"Live session {sessionId} not found.");

        // Transition the YouTube broadcast to "complete" to stop it.
        // If YouTube already stopped it (e.g. OBS disconnected and enableAutoStop fired),
        // the API returns redundantTransition — that's fine, ignore it.
        if (!string.IsNullOrEmpty(session.YouTubeBroadcastId))
        {
            try
            {
                var professor = await professorRepository.GetByIdAsync(session.ProfessorId)
                    ?? throw new KeyNotFoundException("Professor profile not found.");

                var accessToken = await GetValidAccessTokenAsync(professor);
                var http = CreateYouTubeClient(accessToken);
                await TransitionBroadcastAsync(http, session.YouTubeBroadcastId, "complete");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("redundantTransition"))
            {
                // Already ended on YouTube — nothing to do.
            }
        }

        session.Status = LiveSessionStatus.Ended;
        session.EndedAt = DateTime.UtcNow;
        return MapToDto(await liveSessionRepository.UpdateAsync(session));
    }

    public async Task<LiveSessionDto?> GetSessionAsync(Guid sessionId)
    {
        var session = await liveSessionRepository.GetByIdAsync(sessionId);
        return session is null ? null : MapToDto(session);
    }

    public async Task<List<LiveSessionDto>> GetSessionsByProfessorAsync(Guid professorId)
    {
        var sessions = await liveSessionRepository.GetByProfessorAsync(professorId);
        return sessions.Select(MapToDto).ToList();
    }

    public async Task<LiveSessionDto?> GetActiveSessionByCourseAsync(Guid courseId)
    {
        var session = await liveSessionRepository.GetActiveByCourseAsync(courseId);
        return session is null ? null : MapToDto(session);
    }

    public async Task<string> GetStreamKeyAsync(Guid sessionId, Guid professorId)
    {
        var session = await liveSessionRepository.GetByIdAsync(sessionId)
            ?? throw new KeyNotFoundException($"Live session {sessionId} not found.");

        if (string.IsNullOrEmpty(session.YouTubeStreamId))
            throw new InvalidOperationException("No YouTube stream ID associated with this session.");

        var professor = await professorRepository.GetByIdAsync(professorId)
            ?? throw new KeyNotFoundException("Professor profile not found.");

        var accessToken = await GetValidAccessTokenAsync(professor);
        var http = CreateYouTubeClient(accessToken);

        var response = await http.GetAsync(
            $"{YouTubeApiBase}/liveStreams?part=cdn&id={Uri.EscapeDataString(session.YouTubeStreamId)}");

        await EnsureYouTubeSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");
        if (items.GetArrayLength() == 0)
            throw new InvalidOperationException("Stream not found on YouTube.");

        return items[0].GetProperty("cdn").GetProperty("ingestionInfo").GetProperty("streamName").GetString()
            ?? throw new InvalidOperationException("Could not read stream key from YouTube.");
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<string> GetValidAccessTokenAsync(ProfessorProfile professor)
    {
        if (string.IsNullOrEmpty(professor.YouTubeAccessToken))
            throw new InvalidOperationException("YouTube account not connected. Please connect your YouTube account first.");

        if (professor.YouTubeTokenExpiresAt > DateTime.UtcNow)
            return professor.YouTubeAccessToken;

        if (string.IsNullOrEmpty(professor.YouTubeRefreshToken))
            throw new InvalidOperationException("YouTube token expired and no refresh token available. Please reconnect your YouTube account.");

        var http = httpClientFactory.CreateClient();
        var response = await http.PostAsync(GoogleTokenUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["refresh_token"] = professor.YouTubeRefreshToken,
                ["grant_type"] = "refresh_token"
            }));

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<GoogleTokenResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse refresh token response.");

        professor.YouTubeAccessToken = token.AccessToken;
        professor.YouTubeTokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 60);
        professor.UpdatedOn = DateTime.UtcNow;
        await professorRepository.UpdateAsync(professor);

        return professor.YouTubeAccessToken;
    }

    private HttpClient CreateYouTubeClient(string accessToken)
    {
        var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return http;
    }

    private async Task<string> CreateYouTubeBroadcastAsync(
        HttpClient http, string title, string? description, string visibility, DateTime scheduledAt)
    {
        var body = JsonSerializer.Serialize(new
        {
            snippet = new
            {
                title,
                description = description ?? string.Empty,
                scheduledStartTime = scheduledAt.ToString("O")
            },
            status = new { privacyStatus = visibility },
            contentDetails = new { enableAutoStart = true, enableAutoStop = true }
        });

        var response = await http.PostAsync(
            $"{YouTubeApiBase}/liveBroadcasts?part=snippet,status,contentDetails",
            new StringContent(body, Encoding.UTF8, "application/json"));

        await EnsureYouTubeSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("YouTube did not return a broadcast ID.");
    }

    private async Task<(string StreamId, string StreamKey, string RtmpUrl)> CreateYouTubeStreamAsync(
        HttpClient http, string title)
    {
        var body = JsonSerializer.Serialize(new
        {
            snippet = new { title },
            cdn = new { frameRate = "variable", ingestionType = "rtmp", resolution = "variable" }
        });

        var response = await http.PostAsync(
            $"{YouTubeApiBase}/liveStreams?part=snippet,cdn,status",
            new StringContent(body, Encoding.UTF8, "application/json"));

        await EnsureYouTubeSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var streamId = root.GetProperty("id").GetString()!;
        var ingestion = root.GetProperty("cdn").GetProperty("ingestionInfo");
        var streamKey = ingestion.GetProperty("streamName").GetString()!;
        var rtmpUrl = ingestion.GetProperty("ingestionAddress").GetString()!;
        return (streamId, streamKey, rtmpUrl);
    }

    private async Task BindBroadcastToStreamAsync(HttpClient http, string broadcastId, string streamId)
    {
        var response = await http.PostAsync(
            $"{YouTubeApiBase}/liveBroadcasts/bind?id={broadcastId}&part=id,contentDetails&streamId={streamId}",
            null);
        await EnsureYouTubeSuccessAsync(response);
    }

    private async Task TransitionBroadcastAsync(HttpClient http, string broadcastId, string broadcastStatus)
    {
        var response = await http.PostAsync(
            $"{YouTubeApiBase}/liveBroadcasts/transition?broadcastStatus={broadcastStatus}&id={broadcastId}&part=id,status",
            null);
        await EnsureYouTubeSuccessAsync(response);
    }

    private static async Task EnsureYouTubeSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"YouTube API error ({(int)response.StatusCode}): {error}");
        }
    }

    private static string MapVisibility(string? visibility) => visibility?.ToLowerInvariant() switch
    {
        "public" => "public",
        "private" => "private",
        _ => "unlisted"
    };

    private static YouTubeStreamVisibility MapVisibilityEnum(string? visibility) =>
        visibility?.ToLowerInvariant() switch
        {
            "public" => YouTubeStreamVisibility.Public,
            "private" => YouTubeStreamVisibility.Private,
            _ => YouTubeStreamVisibility.Unlisted
        };

    private static LiveSessionDto MapToDto(LiveSession s) => new()
    {
        Id = s.Id,
        CourseId = s.CourseId,
        CourseTitle = s.Course?.Title ?? string.Empty,
        Title = s.Title,
        Description = s.Description,
        EmbedUrl = s.EmbedUrl,
        YouTubeBroadcastId = s.YouTubeBroadcastId,
        Visibility = s.Visibility.ToString(),
        Status = s.Status.ToString(),
        ScheduledAt = s.ScheduledAt,
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt,
        CreatedOn = s.CreatedOn
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class GoogleTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; } = 3600;
    }
}
