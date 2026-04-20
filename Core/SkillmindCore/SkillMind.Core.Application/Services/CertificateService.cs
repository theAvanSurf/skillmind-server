using SkillMind.Core.Application.Dtos.Professor;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Certificates;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;

namespace SkillMind.Core.Application.Services;

public class CertificateService(ICertificateRepository certificateRepository) : ICertificateService
{
    // ── Templates ─────────────────────────────────────────────────────────────

    public async Task<CertificateTemplateDto> CreateTemplateAsync(CreateCertificateTemplateDto dto)
    {
        var key = PredefinedCertificateTemplates.ValidKeys.Contains(dto.TemplateKey)
            ? dto.TemplateKey.ToLowerInvariant()
            : PredefinedCertificateTemplates.Classic;

        var template = new CertificateTemplate
        {
            Id = Guid.NewGuid(),
            ProfessorId = dto.ProfessorId,
            CourseId = dto.CourseId,
            Title = dto.Title,
            Description = dto.Description,
            TemplateKey = key,
            BodyHtml = PredefinedCertificateTemplates.Resolve(key),
            IsDefault = dto.IsDefault,
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
        if (dto.TemplateKey is not null && PredefinedCertificateTemplates.ValidKeys.Contains(dto.TemplateKey))
        {
            template.TemplateKey = dto.TemplateKey.ToLowerInvariant();
            template.BodyHtml = PredefinedCertificateTemplates.Resolve(template.TemplateKey);
        }
        if (dto.IsDefault.HasValue) template.IsDefault = dto.IsDefault.Value;
        if (dto.SignatureUrl is not null) template.SignatureUrl = dto.SignatureUrl;
        if (dto.LogoUrl is not null) template.LogoUrl = dto.LogoUrl;
        if (dto.CompletionThresholdPercent.HasValue)
            template.CompletionThresholdPercent = dto.CompletionThresholdPercent.Value;

        var updated = await certificateRepository.UpdateTemplateAsync(template);
        return MapTemplateToDto(updated);
    }

    // ── Auto-Issuance ─────────────────────────────────────────────────────────

    public async Task<CertificateDto?> TryAutoIssueAsync(Guid studentProfileId, Guid courseId, int progressPercent)
    {
        if (await certificateRepository.HasCertificateAsync(studentProfileId, courseId))
            return null;

        var template = await certificateRepository.GetTemplateByCourseAsync(courseId);
        if (template is null) return null;

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

    public async Task<List<CertificateDto>> GetAllCertificatesByProfessorAsync(Guid professorId)
    {
        var certs = await certificateRepository.GetByProfessorAsync(professorId);
        return certs.Select(c => MapCertToDto(c, c.Template)).ToList();
    }

    public async Task<CertificateDto?> VerifyCertificateAsync(string uniqueCode)
    {
        var cert = await certificateRepository.GetByUniqueCodeAsync(uniqueCode);
        return cert is null ? null : MapCertToDto(cert, cert.Template);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static CertificateTemplateDto MapTemplateToDto(CertificateTemplate t) => new()
    {
        Id = t.Id,
        CourseId = t.CourseId,
        CourseTitle = t.Course?.Title ?? string.Empty,
        TemplateKey = t.TemplateKey,
        IsDefault = t.IsDefault,
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
        SignatureUrl = t?.SignatureUrl,
        TemplateKey = t?.TemplateKey ?? PredefinedCertificateTemplates.Classic
    };

    // ── HTML Rendering ────────────────────────────────────────────────────────

    string ICertificateService.RenderCertificateHtml(CertificateDto cert) => RenderCertificateHtml(cert);

    public static string RenderCertificateHtml(CertificateDto cert)
    {
        var html = PredefinedCertificateTemplates.Resolve(cert.TemplateKey);
        html = html
            .Replace("{{studentName}}", System.Web.HttpUtility.HtmlEncode(cert.StudentName))
            .Replace("{{courseName}}", System.Web.HttpUtility.HtmlEncode(cert.CourseTitle))
            .Replace("{{date}}", cert.IssuedAt.ToString("MMMM d, yyyy"))
            .Replace("{{uniqueCode}}", cert.UniqueCode);

        // Handle {{#if logoUrl}} / {{/if}} blocks
        html = ReplaceConditionalBlock(html, "logoUrl", cert.LogoUrl);
        html = ReplaceConditionalBlock(html, "signatureUrl", cert.SignatureUrl);

        return html;
    }

    private static string ReplaceConditionalBlock(string html, string varName, string? value)
    {
        var open = $"{{{{#if {varName}}}}}";
        var elseTag = "{{else}}";
        var close = "{{/if}}";

        while (html.Contains(open))
        {
            var start = html.IndexOf(open, StringComparison.Ordinal);
            var end = html.IndexOf(close, start, StringComparison.Ordinal);
            if (end < 0) break;

            var inner = html[(start + open.Length)..end];
            string replacement;

            if (!string.IsNullOrWhiteSpace(value))
            {
                var elseIdx = inner.IndexOf(elseTag, StringComparison.Ordinal);
                replacement = elseIdx >= 0
                    ? inner[..elseIdx].Replace($"{{{{{varName}}}}}", System.Web.HttpUtility.HtmlAttributeEncode(value))
                    : inner.Replace($"{{{{{varName}}}}}", System.Web.HttpUtility.HtmlAttributeEncode(value));
            }
            else
            {
                var elseIdx = inner.IndexOf(elseTag, StringComparison.Ordinal);
                replacement = elseIdx >= 0 ? inner[(elseIdx + elseTag.Length)..] : string.Empty;
            }

            html = html[..start] + replacement + html[(end + close.Length)..];
        }

        return html;
    }
}
