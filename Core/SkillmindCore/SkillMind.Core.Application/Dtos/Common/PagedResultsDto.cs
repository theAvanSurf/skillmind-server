namespace SkillMind.Core.Application.Dtos.Common;

public class PagedResultDto<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<T> Data { get; set; } = [];

    public static PagedResultDto<T> Create(IEnumerable<T> data, int totalCount, int page, int pageSize)
    {
        return new PagedResultDto<T>
        {
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}