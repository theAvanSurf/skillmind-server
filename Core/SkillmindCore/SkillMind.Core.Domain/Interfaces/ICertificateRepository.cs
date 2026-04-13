using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Domain.Interfaces;

public interface ICertificateRepository
{
    Task<CertificateTemplate?> GetTemplateByIdAsync(Guid templateId);
    Task<CertificateTemplate?> GetTemplateByCourseAsync(Guid courseId);
    Task<List<CertificateTemplate>> GetTemplatesByProfessorAsync(Guid professorId);
    Task<CertificateTemplate> CreateTemplateAsync(CertificateTemplate template);
    Task<CertificateTemplate> UpdateTemplateAsync(CertificateTemplate template);
    Task<Certificate?> GetByStudentAndCourseAsync(Guid studentProfileId, Guid courseId);
    Task<List<Certificate>> GetByStudentAsync(Guid studentProfileId);
    Task<List<Certificate>> GetByCourseAsync(Guid courseId);
    Task<Certificate> IssueCertificateAsync(Certificate certificate);
    Task<bool> HasCertificateAsync(Guid studentProfileId, Guid courseId);
}
