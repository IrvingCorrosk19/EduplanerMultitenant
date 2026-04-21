using SchoolManager.Dtos;

namespace SchoolManager.Services.Interfaces;

public interface IStudentIdCardService
{
    Task<StudentIdCardDto> GenerateAsync(Guid studentId, Guid createdBy, string? siteBaseUrl = null);
    Task<ScanResultDto> ScanAsync(ScanRequestDto request);

    /// <summary>
    /// Obtiene los datos del carnet activo actual del estudiante sin generar ni revocar nada.
    /// Retorna null si el estudiante no tiene carnet activo o no existe.
    /// Usar en el GET de la vista de generación para no modificar estado en un GET.
    /// </summary>
    Task<StudentIdCardDto?> GetCurrentCardAsync(Guid studentId, string? siteBaseUrl = null);
}
