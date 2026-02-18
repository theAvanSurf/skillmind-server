using System.Text.Json;
using SkillMind.Core.Domain.Interfaces;
using StackExchange.Redis;

namespace SkillMind.Infrastructure.Shared.Services;

public class RedisSet<T>(IDatabase db, string keyPrefix, JsonSerializerOptions jsonOptions) : IRedisSet<T>
{
    private string BuildKey(string id) => $"{keyPrefix}:{id}";

    public async Task SetAsync(string id, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value, jsonOptions);
        await db.StringSetAsync(BuildKey(id), json, expiry, false);
    }

    public async Task<T?> GetAsync(string id)
    {
        var value = await db.StringGetAsync(BuildKey(id));
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString(), jsonOptions);
    }

    public async Task<bool> DeleteAsync(string id)
        => await db.KeyDeleteAsync(BuildKey(id));

    public async Task<bool> ExistsAsync(string id)
        => await db.KeyExistsAsync(BuildKey(id));
}