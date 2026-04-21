using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

public interface IAcademicYearService
{
    Task<AcademicYear?> GetActiveAcademicYearAsync(Guid? schoolId = null);
    Task<AcademicYear?> GetAcademicYearByIdAsync(Guid id);
    Task<List<AcademicYear>> GetAllBySchoolAsync(Guid schoolId);
    Task<AcademicYear> CreateAsync(AcademicYear academicYear);
    Task<AcademicYear> UpdateAsync(AcademicYear academicYear);

    /// <summary>
    /// Garantiza que la escuela tenga al menos un año académico activo.
    /// Si no tiene ninguno, crea uno por defecto para el año actual (1 ene - 31 dic).
    /// </summary>
    Task EnsureDefaultAcademicYearForSchoolAsync(Guid schoolId);
}

