namespace SkillMind.Core.Domain.Interfaces;

public interface IRedisSet<T>
{
    Task SetAsync(string id, T value, TimeSpan? expiry = null);
    Task<T?> GetAsync(string id);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}