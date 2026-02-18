using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Dtos.Profiles;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

public class ProfilesRepository(SkillMindDbContext context) : GenericRepository<Profiles>(context), IProfilesRepository
{
    public IQueryable<Profiles> GetFilteredQuery(string? search, string? sortBy)
    {
        var queryable = context.Profiles.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            queryable = queryable.Where(p => p.ProfileName.Contains(search));

        if (!string.IsNullOrEmpty(sortBy))
        {
            queryable = sortBy switch
            {
                "name" => queryable.OrderBy(p => p.ProfileName),
                "name_desc" => queryable.OrderByDescending(p => p.ProfileName),
                _ => queryable
            };
        }

        return queryable;
    }

    public async Task<(IEnumerable<Profiles>, int)> GetAllByUserId(string userId, PagedQueryDto query)
    {
        if (!Guid.TryParse(userId, out var userGuid))
             throw new ArgumentException("Invalid User ID");

        var queryable = context.Profiles.Where(p => p.UserId == userGuid);
        
        // ... legacy implementation or reuse GetFilteredQuery?
        // if I reuse:
        // var queryable = GetFilteredQuery(query is ProfilesQueryDto q ? q.Search : null, query is ProfilesQueryDto q2 ? q2.SortBy : null);
        // queryable = queryable.Where(p => p.UserId == userId);
        // But let's leave GetAllByUserId as is to avoid breaking changes if I can avoid it, or update it if it's easy.
        // It duplicates logic. Clean code suggests reuse.
        // I will implement GetFilteredQuery and leave GetAllByUserId as is for now to minimize risk, or refactor it.
        // Given I just fixed the missing DTO issue, avoiding changes to legacy method is safer.
        // But wait, the missing DTO issue was because *I* deleted/didn't see the DTO. Now I restored it.
        // So GetAllByUserId should work.
        
        if (query is ProfilesQueryDto profilesQuery)
        {
            if (!string.IsNullOrEmpty(profilesQuery.Search))
                queryable = queryable.Where(p => p.ProfileName.Contains(profilesQuery.Search));

            if (!string.IsNullOrEmpty(profilesQuery.SortBy))
                queryable = profilesQuery.SortBy switch
                {
                    "name" => queryable.OrderBy(p => p.ProfileName),
                    "name_desc" => queryable.OrderByDescending(p => p.ProfileName),
                    _ => queryable
                };
        }

        var totalCount = await queryable.CountAsync();
        var data = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}