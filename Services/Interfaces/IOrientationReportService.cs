using SchoolManager.Models;
using SchoolManager.Dtos;

public interface IOrientationReportService
{
    Task<List<OrientationReport>> GetAllAsync();
    Task<OrientationReport?> GetByIdAsync(Guid? id);
    Task CreateAsync(OrientationReport report);
    Task UpdateAsync(OrientationReport report);
    Task DeleteAsync(Guid id);
    Task<List<OrientationReport>> GetByStudentAsync(Guid studentId);
    Task<List<OrientationReport>> GetFilteredAsync(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId, Guid? groupId = null, Guid? studentId = null);
    Task<List<OrientationReportDto>> GetByStudentDtoAsync(Guid studentId, string trimester = null);
    Task<List<OrientationReportDto>> GetByCounselorAsync(Guid counselorId, string trimester = null);
}
