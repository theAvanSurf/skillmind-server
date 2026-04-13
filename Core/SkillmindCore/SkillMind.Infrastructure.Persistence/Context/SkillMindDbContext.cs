using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

namespace SkillMind.Infrastructure.Persistence.Context;

public class SkillMindDbContext(DbContextOptions<SkillMindDbContext> options) : DbContext(options)
{
    // Existing
    public DbSet<Profiles> Profiles { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<CourseProgress> CourseProgresses { get; set; }

    // Professor features
    public DbSet<ProfessorProfile> ProfessorProfiles { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<CertificateTemplate> CertificateTemplates { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<ExamAttempt> ExamAttempts { get; set; }
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }
    public DbSet<LiveSession> LiveSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Existing
        modelBuilder.ApplyConfiguration(new ProfilesEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CourseEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SeasonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LessonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CourseProgressEntityConfiguration());

        // Professor features
        modelBuilder.ApplyConfiguration(new ProfessorProfileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EnrollmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CertificateTemplateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CertificateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExamEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExamQuestionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionOptionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExamAttemptEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AttemptAnswerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LiveSessionEntityConfiguration());
    }
}