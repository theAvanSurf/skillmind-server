using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.Infrastructure.Persistence.Services;

public class PaginationService : IPaginationService
{
    public async Task<PagedResultDto<T>> PaginateAsync<T>(IQueryable<T> query, PagedQueryDto paginationParams)
    {
        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        return PagedResultDto<T>.Create(items, totalCount, paginationParams.Page, paginationParams.PageSize);
    }
}
