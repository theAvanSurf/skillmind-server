
using SkillMind.Infrastructure.Identity;
using SkillMind.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddIdentityLayer(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddSwaggerExtension();
builder.Services.AddApiVersioningExtension();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
await app.Services.SeedDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerExtension(app);
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();