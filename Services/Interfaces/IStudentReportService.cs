using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces
{
    public interface IStudentReportService
    {
        Task<StudentReportDto> GetReportByStudentIdAsync(Guid studentId);
        Task<StudentReportDto> GetReportByStudentIdAndTrimesterAsync(Guid studentId, string trimester);
        Task<List<DisciplineReportDto>> GetDisciplineReportsByStudentIdAsync(Guid studentId);
    }
}
