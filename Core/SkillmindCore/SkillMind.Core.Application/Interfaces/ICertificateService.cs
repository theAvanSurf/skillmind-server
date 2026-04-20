using SkillMind.Core.Application.Dtos.Professor;

namespace SkillMind.Core.Application.Interfaces;

public interface ICertificateService
{
    // Templates
    Task<CertificateTemplateDto> CreateTemplateAsync(CreateCertificateTemplateDto dto);
    Task<CertificateTemplateDto?> GetTemplateByIdAsync(Guid templateId);
    Task<CertificateTemplateDto?> GetTemplateByCourseAsync(Guid courseId);
    Task<List<CertificateTemplateDto>> GetTemplatesByProfessorAsync(Guid professorId);
    Task<CertificateTemplateDto> UpdateTemplateAsync(Guid templateId, UpdateCertificateTemplateDto dto);

    // Issuance
    Task<CertificateDto?> TryAutoIssueAsync(Guid studentProfileId, Guid courseId, int progressPercent);
    Task<CertificateDto> ManualIssueAsync(ManualIssueCertificateDto dto);
    Task<List<CertificateDto>> GetCertificatesByStudentAsync(Guid studentProfileId);
    Task<List<CertificateDto>> GetCertificatesByCourseAsync(Guid courseId);
    Task<List<CertificateDto>> GetAllCertificatesByProfessorAsync(Guid professorId);
    Task<CertificateDto?> VerifyCertificateAsync(string uniqueCode);
}
