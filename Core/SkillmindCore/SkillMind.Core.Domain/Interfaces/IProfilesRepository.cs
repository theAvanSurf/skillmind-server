using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Domain.Interfaces;

public interface IProfilesRepository : IGenericRepository<Profiles>
{
    IQueryable<Profiles> GetFilteredQuery(string? search, string? sortBy);
}