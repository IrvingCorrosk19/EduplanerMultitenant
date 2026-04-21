using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SchoolManager.Dtos;
using SchoolManager.Models;   // contiene la entidad Activity


namespace SchoolManager.Interfaces
{
    public interface IActivityService
    {
        /* Operaciones para el Portal del Docente */
        Task<ActivityDto> CreateAsync(ActivityCreateDto dto);
        Task<ActivityDto> UpdateAsync(ActivityUpdateDto dto);
        Task<IEnumerable<ActivityHeaderDto>> GetByTeacherGroupTrimesterAsync(
            Guid teacherId, Guid groupId, string trimesterCode, Guid subjectId, Guid gradeLevelId);
        Task UploadPdfAsync(Guid activityId, string fileName, Stream content);

        /* CRUD legado (opcional) */
        Task<List<Activity>> GetAllAsync();
        Task<Activity?> GetByIdAsync(Guid id);
        Task UpdateAsync(Activity activity);
        Task DeleteAsync(Guid id);
        Task<List<Activity>> GetByGroupAndSubjectAsync(Guid groupId, Guid subjectId);
    }
}
