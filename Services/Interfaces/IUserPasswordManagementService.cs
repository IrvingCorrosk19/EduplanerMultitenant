using SchoolManager.Dtos;
using SchoolManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SchoolManager.Services.Interfaces
{
    public interface IUserPasswordManagementService
    {
        Task<List<UserListDto>> GetAllUsersAsync();
        Task<List<UserListDto>> GetUsersByRoleAsync(string role);

        /// <summary>Listados para Index con filtros por grado/grupo (query string).</summary>
        Task<UserPasswordManagementIndexViewModel> GetIndexViewModelAsync(
            Guid? gradeId,
            Guid? groupId,
            string? roleFilter,
            string? searchQuery,
            Guid? callerSchoolId,
            bool callerIsSuperAdmin,
            CancellationToken cancellationToken = default);
    }
}
