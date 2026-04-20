using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

public class CertificateRepository(SkillMindDbContext context)
    : GenericRepository<CertificateTemplate>(context), ICertificateRepository
{
    public async Task<CertificateTemplate?> GetTemplateByIdAsync(Guid templateId)
    {
        return await context.CertificateTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId);
    }

    public async Task<CertificateTemplate?> GetTemplateByCourseAsync(Guid courseId)
    {
        return await context.CertificateTemplates
            .FirstOrDefaultAsync(t => t.CourseId == courseId);
    }

    public async Task<List<CertificateTemplate>> GetTemplatesByProfessorAsync(Guid professorId)
    {
        return await context.CertificateTemplates
            .Where(t => t.ProfessorId == professorId)
            .OrderByDescending(t => t.CreatedOn)
            .ToListAsync();
    }

    public async Task<CertificateTemplate> CreateTemplateAsync(CertificateTemplate template)
    {
        await context.CertificateTemplates.AddAsync(template);
        await context.SaveChangesAsync();
        return template;
    }

    public async Task<CertificateTemplate> UpdateTemplateAsync(CertificateTemplate template)
    {
        context.CertificateTemplates.Update(template);
        await context.SaveChangesAsync();
        return template;
    }

    public async Task<Certificate?> GetByStudentAndCourseAsync(Guid studentProfileId, Guid courseId)
    {
        return await context.Certificates
            .Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.StudentProfileId == studentProfileId && c.CourseId == courseId);
    }

    public async Task<List<Certificate>> GetByStudentAsync(Guid studentProfileId)
    {
        return await context.Certificates
            .Include(c => c.Course)
            .Include(c => c.Template)
            .Where(c => c.StudentProfileId == studentProfileId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();
    }

    public async Task<List<Certificate>> GetByCourseAsync(Guid courseId)
    {
        return await context.Certificates
            .Include(c => c.StudentProfile)
            .Where(c => c.CourseId == courseId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();
    }

    public async Task<Certificate> IssueCertificateAsync(Certificate certificate)
    {
        await context.Certificates.AddAsync(certificate);
        await context.SaveChangesAsync();
        return certificate;
    }

    public async Task<bool> HasCertificateAsync(Guid studentProfileId, Guid courseId)
    {
        return await context.Certificates
            .AnyAsync(c => c.StudentProfileId == studentProfileId && c.CourseId == courseId);
    }

    public async Task<Certificate?> GetByUniqueCodeAsync(string uniqueCode)
    {
        return await context.Certificates
            .Include(c => c.Template)
            .Include(c => c.Course)
            .Include(c => c.StudentProfile)
            .FirstOrDefaultAsync(c => c.UniqueCode == uniqueCode);
    }

    public async Task<List<Certificate>> GetByProfessorAsync(Guid professorId)
    {
        return await context.Certificates
            .Include(c => c.Course)
            .Include(c => c.Template)
            .Include(c => c.StudentProfile)
            .Where(c => c.Course.ProfessorId == professorId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();
    }
}
