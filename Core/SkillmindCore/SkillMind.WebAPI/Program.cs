using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Application;
using SkillMind.Infrastructure.Identity;
using SkillMind.Infrastructure.Identity.Contexts;
using SkillMind.Infrastructure.Persistence;
using SkillMind.Infrastructure.Persistence.Context;
using SkillMind.Infrastructure.Shared;
using SkillMind.WebAPI.Extensions;
using SkillMind.WebAPI.Transformers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddIdentityLayer(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddSwaggerExtension();
builder.Services.AddApiVersioningExtension();
builder.Services.AddPersistenceLayerIoc(builder.Configuration);
builder.Services.AddSharedLayer(builder.Configuration);
builder.Services.AddApplicationLayer();

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
});
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await MigrateWithRetryAsync(async () =>
    {
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
        await identityDb.Database.MigrateAsync();
    });

    await MigrateWithRetryAsync(async () =>
    {
        var persistenceDb = scope.ServiceProvider.GetRequiredService<SkillMindDbContext>();
        await persistenceDb.Database.MigrateAsync();
    });
}

await app.Services.SeedDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerExtension(app);
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task MigrateWithRetryAsync(Func<Task> migrate, int maxRetries = 5)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await migrate();
            return;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s, 8s, 16s...
            Console.WriteLine($"[Migration] Attempt {attempt} failed: {ex.Message}. Retrying in {delay.TotalSeconds}s...");
            await Task.Delay(delay);
        }
    }
    // Last attempt — let it throw naturally
    await migrate();
}