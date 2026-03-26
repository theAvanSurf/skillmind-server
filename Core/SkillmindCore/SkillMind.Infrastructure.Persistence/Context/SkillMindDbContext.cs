using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

namespace SkillMind.Infrastructure.Persistence.Context;

public class SkillMindDbContext(DbContextOptions<SkillMindDbContext> options) : DbContext(options)
{
    public DbSet<Profiles> Profiles { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<CourseProgress> CourseProgresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProfilesEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CourseEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SeasonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LessonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CourseProgressEntityConfiguration());
    }
}