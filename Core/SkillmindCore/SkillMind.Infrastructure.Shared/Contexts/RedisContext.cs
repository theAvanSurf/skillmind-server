using System.Text.Json;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Shared.Services;
using StackExchange.Redis;

namespace SkillMind.Infrastructure.Shared.Contexts;

public class RedisContext : IRedisContext
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisContext(string connectionString = "localhost:6379")
    {
        _connection = ConnectionMultiplexer.Connect(connectionString);
        _database = _connection.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public IRedisSet<T> Set<T>(string keyPrefix) => new RedisSet<T>(_database, keyPrefix, _jsonOptions);

    public IDatabase Database => _database;

    public ISubscriber Subscriber => _connection.GetSubscriber();

    public async ValueTask DisposeAsync()
    {
        await _connection.CloseAsync();
        _connection.Dispose();
    }
}