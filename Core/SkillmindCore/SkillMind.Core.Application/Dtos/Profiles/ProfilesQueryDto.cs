using SkillMind.Core.Application.Dtos.Common;

namespace SkillMind.Core.Application.Dtos.Profiles;

public class ProfilesQueryDto : PagedQueryDto
{
    public string? Search { get; set; }
    public string? SortBy { get; set; }
}
