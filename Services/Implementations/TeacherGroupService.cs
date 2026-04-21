using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations
{
    public class TeacherGroupService : ITeacherGroupService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public TeacherGroupService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<GroupDto>> GetByTeacherAsync(Guid teacherId, string trimesterCode)
        {
            return await _context.TeacherAssignments
                .Where(ta => ta.TeacherId == teacherId)
                .Include(ta => ta.SubjectAssignment)      // 1er salto
                    .ThenInclude(sa => sa.Group)          // 2º salto
                .Select(ta => ta.SubjectAssignment.Group)
                .Distinct()
                .Select(g => new GroupDto
                {
                    Id = g.Id,
                    DisplayName = $"{g.Grade} – {g.Name}" // «1° – A»
                })
                .ToListAsync();
        }
    }
}
