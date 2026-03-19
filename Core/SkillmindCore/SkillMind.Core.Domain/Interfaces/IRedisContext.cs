namespace SkillMind.Core.Domain.Interfaces;

public interface IRedisContext : IAsyncDisposable
{
    IRedisSet<T> Set<T>(string keyPrefix);
}