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

        var rawConnectionString = configuration.GetConnectionString("DatabaseUrl") ?? "";
        var connectionString = ParsePostgresUri(rawConnectionString);

        services.AddDbContext<SkillMindDbContext>(options =>
        {
            options.UseNpgsql(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(SkillMindDbContext).Assembly.FullName);
            });
        });

        #endregion

        #region Services Registration

        services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddTransient<IProfilesRepository, ProfilesRepository>();
        services.AddTransient<ICourseRepository, CourseRepository>();
        
        // Professor Features
        services.AddTransient<IProfessorRepository, ProfessorRepository>();
        services.AddTransient<IExamRepository, ExamRepository>();
        services.AddTransient<ICertificateRepository, CertificateRepository>();
        services.AddTransient<ILiveSessionRepository, LiveSessionRepository>();

        services.AddTransient<IPaginationService, PaginationService>();

        #endregion
    }

    /// <summary>
    /// Converts a postgres:// or postgresql:// URI to a Npgsql key=value connection string.
    /// Returns the string unchanged if it is already in key=value format.
    /// </summary>
    private static string ParsePostgresUri(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;
        if (!connectionString.StartsWith("postgresql://") && !connectionString.StartsWith("postgres://"))
            return connectionString;

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}