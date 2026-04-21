using System.Threading.Tasks;
using SchoolManager.ViewModels;
using System.Collections.Generic;

namespace SchoolManager.Services.Interfaces
{
    public interface IDirectorService
    {
        Task<DirectorViewModel> GetDashboardViewModelAsync(string trimestre = null);
        Task<PagedResult<MateriaDesempenoViewModel>> GetMateriasDesempenoAsync(int page, int pageSize, string trimestre = null);
        Task<PagedResult<ProfesorDesempenoViewModel>> GetProfesoresDesempenoAsync(int page, int pageSize, string trimestre = null);
        Task<PagedResult<MateriaAprobacionViewModel>> GetMateriasAprobacionAsync(int page, int pageSize, string trimestre = null);
        Task<PagedResult<AlertaNotificacionViewModel>> GetAlertasAsync(int page, int pageSize, string trimestre = null);
        Task<DirectorViewModel> GetInitialTotalsAsync();
    }
} 