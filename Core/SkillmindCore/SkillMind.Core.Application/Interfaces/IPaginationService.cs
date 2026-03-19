using SkillMind.Core.Application.Dtos.Common;

namespace SkillMind.Core.Application.Interfaces;

public interface IPaginationService
{
    Task<PagedResultDto<T>> PaginateAsync<T>(IQueryable<T> query, PagedQueryDto paginationParams);
}
