using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolManager.Dtos;
using System;

namespace SchoolManager.Services.Interfaces
{
    public interface ITrimesterService
    {
        Task<List<TrimesterDto>> GetAllAsync();
        Task<TrimesterValidationDto> ValidarTrimestresAsync(List<TrimesterDto> trimestres);
        Task GuardarTrimestresAsync(List<TrimesterDto> trimestres);
        Task<bool> EditarFechasTrimestreAsync(TrimesterDto dto);
        Task EliminarTodosLosTrimestresAsync();
        Task<List<TrimesterDto>> GetTrimestresPorAnioEscolarAsync(int anioEscolar);
        Task<bool> IsTrimesterActiveAsync(string trimesterName);
        Task ValidateTrimesterActiveAsync(string trimesterName);
        Task<bool> ActivarTrimestreAsync(Guid id);
        Task<bool> DesactivarTrimestreAsync(Guid id);
    }
}
