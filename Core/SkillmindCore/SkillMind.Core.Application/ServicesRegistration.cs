using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Application.Services;

namespace SkillMind.Core.Application;

public static class ServiceInjection
{
    public static void AddApplicationLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { }, typeof(ServiceInjection));
        services.AddTransient<IProfilesServices, ProfilesServices>();
        services.AddTransient<ISessionManager, SessionManager>();
        services.AddTransient<ICourseService, CoursesService>();

        // Professor Features
        services.AddTransient<IProfessorService, ProfessorService>();
        services.AddTransient<IExamService, ExamService>();
        services.AddTransient<ICertificateService, CertificateService>();
        // ILiveStreamService registered in Infrastructure.Shared (needs HTTP client + config)
    }
}