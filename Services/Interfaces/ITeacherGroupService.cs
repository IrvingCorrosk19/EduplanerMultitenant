using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolManager.Dtos;

namespace SchoolManager.Interfaces
{
    public interface ITeacherGroupService
    {
        Task<IEnumerable<GroupDto>> GetByTeacherAsync(Guid teacherId, string trimesterCode);
    }
}
