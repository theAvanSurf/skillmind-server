using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Shared.Contexts;

namespace SkillMind.Infrastructure.Shared;

public static class SharedLayerInjection
{
    public static void AddSharedLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IRedisContext>(_ => new RedisContext(connectionString));
    }
}