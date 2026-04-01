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
    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDatabaseContext>();
    await identityDb.Database.MigrateAsync();

    var persistenceDb = scope.ServiceProvider.GetRequiredService<SkillMindDbContext>();
    await persistenceDb.Database.MigrateAsync();
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