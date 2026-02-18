namespace SkillMind.Core.Domain.Interfaces;

public interface IGenericRepository<TEntity> where TEntity : class
{
    public Task<TEntity> CreateAsync(TEntity entity);
    public Task<TEntity> UpdateAsync(Guid id, TEntity entity);
    public Task<List<TEntity>> GetAllAsync();
    public Task<TEntity?> GetByIdAsync(Guid id);
    public IQueryable<TEntity> GetAllQuery();
    public Task<bool> DeleteAsync(Guid id);
    public Task<List<TEntity>> GetAllListWithInclude(List<string> properties);
    public IQueryable<TEntity> GetAllLQueryWithInclude(List<string> properties);
    Task<List<TEntity>> CreateRangeAsync(List<TEntity> entities);
}