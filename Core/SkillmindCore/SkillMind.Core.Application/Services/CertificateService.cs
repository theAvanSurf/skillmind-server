using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class CertificateService(ICertificateRepository certificateRepository) : ICertificateService
{
    // ── Templates ─────────────────────────────────────────────────────────────

    public async Task<CertificateTemplateDto> CreateTemplateAsync(CreateCertificateTemplateDto dto)
    {
        var template = new CertificateTemplate
        {
            Id = Guid.NewGuid(),
            ProfessorId = dto.ProfessorId,
            CourseId = dto.CourseId,
            Title = dto.Title,
            Description = dto.Description,
            SignatureUrl = dto.SignatureUrl,
            LogoUrl = dto.LogoUrl,
            CompletionThresholdPercent = dto.CompletionThresholdPercent,
            CreatedOn = DateTime.UtcNow
        };
        var created = await certificateRepository.CreateTemplateAsync(template);
        return MapTemplateToDto(created);
    }

    public async Task<CertificateTemplateDto?> GetTemplateByIdAsync(Guid templateId)
    {
        var template = await certificateRepository.GetTemplateByIdAsync(templateId);
        return template is null ? null : MapTemplateToDto(template);
    }

    public async Task<CertificateTemplateDto?> GetTemplateByCourseAsync(Guid courseId)
    {
        var template = await certificateRepository.GetTemplateByCourseAsync(courseId);
        return template is null ? null : MapTemplateToDto(template);
    }

    public async Task<List<CertificateTemplateDto>> GetTemplatesByProfessorAsync(Guid professorId)
    {
        var templates = await certificateRepository.GetTemplatesByProfessorAsync(professorId);
        return templates.Select(MapTemplateToDto).ToList();
    }

    public async Task<CertificateTemplateDto> UpdateTemplateAsync(Guid templateId, UpdateCertificateTemplateDto dto)
    {
        var template = await certificateRepository.GetTemplateByIdAsync(templateId)
            ?? throw new KeyNotFoundException($"Certificate template {templateId} not found.");

        if (dto.Title is not null) template.Title = dto.Title;
        if (dto.Description is not null) template.Description = dto.Description;
        if (dto.SignatureUrl is not null) template.SignatureUrl = dto.SignatureUrl;
        if (dto.LogoUrl is not null) template.LogoUrl = dto.LogoUrl;
        if (dto.CompletionThresholdPercent.HasValue)
            template.CompletionThresholdPercent = dto.CompletionThresholdPercent.Value;

        var updated = await certificateRepository.UpdateTemplateAsync(template);
        return MapTemplateToDto(updated);
    }

    // ── Auto-Issuance (triggered from CoursesService.UpdateProgressAsync) ─────

    public async Task<CertificateDto?> TryAutoIssueAsync(Guid studentProfileId, Guid courseId, int progressPercent)
    {
        // Already has a certificate — skip
        if (await certificateRepository.HasCertificateAsync(studentProfileId, courseId))
            return null;

        var template = await certificateRepository.GetTemplateByCourseAsync(courseId);
        if (template is null) return null;

        // Progress hasn't reached the threshold — skip
        if (progressPercent < template.CompletionThresholdPercent) return null;

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            StudentProfileId = studentProfileId,
            CourseId = courseId,
            UniqueCode = Guid.NewGuid().ToString("N").ToUpperInvariant(),
            IsManuallyIssued = false,
            IssuedAt = DateTime.UtcNow
        };

        var issued = await certificateRepository.IssueCertificateAsync(certificate);
        return MapCertToDto(issued, template);
    }

    // ── Manual Issuance ───────────────────────────────────────────────────────

    public async Task<CertificateDto> ManualIssueAsync(ManualIssueCertificateDto dto)
    {
        if (await certificateRepository.HasCertificateAsync(dto.StudentProfileId, dto.CourseId))
            throw new InvalidOperationException("A certificate has already been issued for this student and course.");

        var template = await certificateRepository.GetTemplateByIdAsync(dto.TemplateId)
            ?? throw new KeyNotFoundException($"Certificate template {dto.TemplateId} not found.");

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            TemplateId = dto.TemplateId,
            StudentProfileId = dto.StudentProfileId,
            CourseId = dto.CourseId,
            UniqueCode = Guid.NewGuid().ToString("N").ToUpperInvariant(),
            IsManuallyIssued = true,
            IssuedAt = DateTime.UtcNow
        };

        var issued = await certificateRepository.IssueCertificateAsync(certificate);
        return MapCertToDto(issued, template);
    }

    public async Task<List<CertificateDto>> GetCertificatesByStudentAsync(Guid studentProfileId)
    {
        var certs = await certificateRepository.GetByStudentAsync(studentProfileId);
        return certs.Select(c => MapCertToDto(c, c.Template)).ToList();
    }

    public async Task<List<CertificateDto>> GetCertificatesByCourseAsync(Guid courseId)
    {
        var certs = await certificateRepository.GetByCourseAsync(courseId);
        return certs.Select(c => MapCertToDto(c, c.Template)).ToList();
    }

    public async Task<CertificateDto?> VerifyCertificateAsync(string uniqueCode)
    {
        // TODO: Implement public verification endpoint in Sprint 7
        await Task.CompletedTask;
        return null;
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static CertificateTemplateDto MapTemplateToDto(CertificateTemplate t) => new()
    {
        Id = t.Id,
        CourseId = t.CourseId,
        CourseTitle = t.Course?.Title ?? string.Empty,
        Title = t.Title,
        Description = t.Description,
        SignatureUrl = t.SignatureUrl,
        LogoUrl = t.LogoUrl,
        CompletionThresholdPercent = t.CompletionThresholdPercent,
        IssuedCount = t.Certificates?.Count ?? 0,
        CreatedOn = t.CreatedOn
    };

    private static CertificateDto MapCertToDto(Certificate c, CertificateTemplate? t) => new()
    {
        Id = c.Id,
        CourseId = c.CourseId,
        CourseTitle = c.Course?.Title ?? string.Empty,
        StudentName = c.StudentProfile?.ProfileName ?? string.Empty,
        UniqueCode = c.UniqueCode,
        IsManuallyIssued = c.IsManuallyIssued,
        IssuedAt = c.IssuedAt,
        LogoUrl = t?.LogoUrl,
        SignatureUrl = t?.SignatureUrl
    };
}
