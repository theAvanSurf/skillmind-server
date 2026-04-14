using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Identity.Contexts;
using SkillMind.Infrastructure.Identity.Entities;
using SkillMind.Infrastructure.Identity.Services;
using SkillMind.Infrastructure.Shared;

namespace SkillMind.Infrastructure.Identity;

public static class ServicesRegistration
{
    public static void AddIdentityLayer(this IServiceCollection service, IConfiguration configuration)
    {
        GeneralContextConfiguration(service, configuration);

        service.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        service.Configure<CredentialChangeSettings>(configuration.GetSection("CredentialChange"));

        #region Identity
        service.Configure<IdentityOptions>(opt =>
        {
            opt.Password.RequiredLength = 8;
            opt.Password.RequireDigit = true;
            opt.Password.RequireNonAlphanumeric = true;
            opt.Password.RequireLowercase = true;
            opt.Password.RequireUppercase = true;

            opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            opt.Lockout.MaxFailedAccessAttempts = 5;

            opt.User.RequireUniqueEmail = true;
            opt.SignIn.RequireConfirmedEmail = true;
        });

        service.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<IdentityDatabaseContext>()
            .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(TokenOptions.DefaultProvider);

        service.Configure<DataProtectionTokenProviderOptions>(opt =>
        {
            opt.TokenLifespan = TimeSpan.FromHours(12);
        });

        service.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(opt =>
        {
            opt.RequireHttpsMetadata = false;
            opt.SaveToken = false;
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidAudience = configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        configuration["JwtSettings:SecretKey"]
                        ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.")
                    )
                )
            };
            opt.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = async af =>
                {
                    af.NoResult();

                    if (!af.Response.HasStarted)
                    {
                        af.Response.StatusCode = 500;
                        af.Response.ContentType = "text/plain";
                        await af.Response.WriteAsync(af.Exception.Message);
                    }
                },
                OnChallenge = async c =>
                {
                    c.HandleResponse();

                    if (!c.Response.HasStarted)
                    {
                        c.Response.StatusCode = 401;
                        c.Response.ContentType = "application/json";
                        var result = JsonConvert.SerializeObject(new JwtResponseDto
                        {
                            HasError = true,
                            Error = "You are not Authorized"
                        });
                        await c.Response.WriteAsync(result);
                    }
                },
                OnForbidden = async c =>
                {
                    if (!c.Response.HasStarted)
                    {
                        c.Response.StatusCode = 403;
                        c.Response.ContentType = "application/json";
                        var result = JsonConvert.SerializeObject(new JwtResponseDto
                        {
                            HasError = true,
                            Error = "You are not Authorized to access this resource"
                        });
                        await c.Response.WriteAsync(result);
                    }
                }
            };
        }).AddCookie(IdentityConstants.ApplicationScheme, opt =>
        {
            opt.ExpireTimeSpan = TimeSpan.FromMinutes(180);
        });
        #endregion


        #region Services Registration

        service.AddScoped<IAccountServicesApi, AccountServices>();
        #endregion
    }

    private static void GeneralContextConfiguration(IServiceCollection services, IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("DatabaseUrl") ?? "";
        var connectionString = ParsePostgresUri(rawConnectionString);

        services.AddDbContext<IdentityDatabaseContext>(options =>
            options.UseNpgsql(
                connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(IdentityDatabaseContext).Assembly.FullName)
            )
        );
    }

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
    
    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var servicesProvider = scope.ServiceProvider;

        var roleManager = servicesProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedAsync(roleManager);
    }

    private static async Task SeedAsync(RoleManager<IdentityRole> roles)
{
    string[] defaultRoles = [nameof(Roles.Admin), nameof(Roles.Professor), nameof(Roles.Student)];

    foreach (var role in defaultRoles)
    {
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new IdentityRole(role));
    }
}
    
}