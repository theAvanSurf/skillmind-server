using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;
using SkillMind.Infrastructure.Persistence.Repositories;
using SkillMind.Infrastructure.Persistence.Services;

namespace SkillMind.Infrastructure.Persistence;

public static class ServicesInjection
{
    public static void AddPersistenceLayerIoc(this IServiceCollection services, IConfiguration configuration)
    {
        #region Context

        services.AddDbContext<SkillMindDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DatabaseUrl"), sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(SkillMindDbContext).Assembly.FullName);
            });
        });

        #endregion

        #region Services Registration

        services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddTransient<IProfilesRepository, ProfilesRepository>();
        services.AddTransient<IPaginationService, PaginationService>();

        #endregion
    }
}