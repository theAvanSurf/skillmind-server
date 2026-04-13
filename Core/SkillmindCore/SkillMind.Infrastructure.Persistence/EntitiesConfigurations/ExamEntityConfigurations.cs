using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillMind.Core.Domain.Entities;

namespace SkillMind.Infrastructure.Persistence.EntitiesConfigurations;

public class ExamEntityConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable(nameof(Exam));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.PassingScore).IsRequired();

        builder.HasOne(x => x.Course)
            .WithMany(c => c.Exams)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Questions)
            .WithOne(q => q.Exam)
            .HasForeignKey(q => q.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Attempts)
            .WithOne(a => a.Exam)
            .HasForeignKey(a => a.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExamQuestionEntityConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable(nameof(ExamQuestion));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuestionText).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.QuestionType).IsRequired();
        builder.Property(x => x.Points).IsRequired();
        builder.Property(x => x.Order).IsRequired();

        builder.HasMany(x => x.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuestionOptionEntityConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable(nameof(QuestionOption));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OptionText).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Order).IsRequired();
    }
}

public class ExamAttemptEntityConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.ToTable(nameof(ExamAttempt));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProfessorFeedback).HasMaxLength(5000);
        builder.Property(x => x.StartedAt).IsRequired();

        builder.HasOne(x => x.StudentProfile)
            .WithMany()
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Answers)
            .WithOne(a => a.Attempt)
            .HasForeignKey(a => a.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttemptAnswerEntityConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
    {
        builder.ToTable(nameof(AttemptAnswer));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TextAnswer).HasMaxLength(5000);

        builder.HasOne(x => x.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SelectedOption)
            .WithMany()
            .HasForeignKey(x => x.SelectedOptionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
