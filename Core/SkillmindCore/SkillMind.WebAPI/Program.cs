
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using SkillMind.Core.Application;
using SkillMind.Infrastructure.Identity;
using SkillMind.Infrastructure.Persistence;
using SkillMind.Infrastructure.Shared;
using SkillMind.WebAPI.Extensions;
using SkillMind.WebAPI.Transformers;
using SkillMind.Infrastructure.Shared;

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