using SchoolManager.ViewModels;

namespace SchoolManager.Services.Interfaces
{
    /// <summary>
    /// Servicio para gestionar el perfil de los estudiantes
    /// </summary>
    public interface IStudentProfileService
    {
        /// <summary>
        /// Obtiene el perfil del estudiante actual
        /// </summary>
        Task<StudentProfileViewModel?> GetStudentProfileAsync(Guid studentId);

        /// <summary>
        /// Actualiza el perfil del estudiante
        /// </summary>
        Task<bool> UpdateStudentProfileAsync(StudentProfileViewModel model);

        /// <summary>
        /// Valida que el email no esté en uso por otro usuario
        /// </summary>
        Task<bool> IsEmailAvailableAsync(string email, Guid currentUserId);

        /// <summary>
        /// Valida que el documento de identidad no esté en uso por otro usuario
        /// </summary>
        Task<bool> IsDocumentIdAvailableAsync(string? documentId, Guid currentUserId);
    }
}

