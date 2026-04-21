using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services
{
    public class StudentService : IStudentService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StudentService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<Student>> GetAllAsync()
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null) return new List<Student>();
            return await _context.Students
                .Where(s => s.SchoolId == schoolId)
                .ToListAsync();
        }

        public async Task<Student?> GetByIdAsync(Guid id)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            var student = await _context.Students.FindAsync(id);
            if (student == null || student.SchoolId != schoolId) return null;
            return student;
        }

        public async Task CreateAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            var student = await _context.Students.FindAsync(id);
            if (student == null || student.SchoolId != schoolId) return;
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Student>> GetByGroupAsync(string groupName)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null)
                return new List<Student>();
            return await _context.Students
                .Where(s => s.GroupName == groupName && s.SchoolId == schoolId)
                .ToListAsync();
        }

        public async Task<IEnumerable<StudentBasicDto>> GetByGroupAndGradeAsync(Guid groupId, Guid gradeId)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null)
                return Enumerable.Empty<StudentBasicDto>();

            // MEJORADO: Filtrar solo estudiantes con asignaciones activas
            var result = await (from sa in _context.StudentAssignments
                                join student in _context.Users on sa.StudentId equals student.Id
                                join grade in _context.GradeLevels on sa.GradeId equals grade.Id
                                join grupo in _context.Groups on sa.GroupId equals grupo.Id
                                where (student.Role == "estudiante" || student.Role == "student" || student.Role == "alumno")
                                      && student.SchoolId == schoolId
                                      && grade.SchoolId == schoolId
                                      && grupo.SchoolId == schoolId
                                      && sa.GroupId == groupId
                                      && sa.GradeId == gradeId
                                      && sa.IsActive // Solo asignaciones activas
                                orderby student.LastName, student.Name
                                select new StudentBasicDto
                                {
                                    StudentId = student.Id,
                                    FullName = $"{student.LastName}, {student.Name}",  // Apellido, Nombre
                                    GradeName = grade.Name,
                                    GroupName = grupo.Name,
                                    DocumentId = student.DocumentId ?? ""
                                }).ToListAsync();

            return result;
        }

        public async Task<IEnumerable<StudentBasicDto>> GetBySubjectGroupAndGradeAsync(Guid subjectId, Guid groupId, Guid gradeId)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null)
                return Enumerable.Empty<StudentBasicDto>();

            // Misma lógica que GetByGroupAndGradeAsync, pero solo si existe al menos una asignación de
            // la materia a ese grupo/grado. Se usa EXISTS en lugar de JOIN a subject_assignments: varias
            // filas (p. ej. distinto area_id) duplicaban cada estudiante en el resultado.
            var result = await (from sa in _context.StudentAssignments
                                join student in _context.Users on sa.StudentId equals student.Id
                                join grade in _context.GradeLevels on sa.GradeId equals grade.Id
                                join grupo in _context.Groups on sa.GroupId equals grupo.Id
                                where (student.Role == "estudiante" || student.Role == "student" || student.Role == "alumno")
                                      && student.SchoolId == schoolId
                                      && grade.SchoolId == schoolId
                                      && grupo.SchoolId == schoolId
                                      && sa.GroupId == groupId
                                      && sa.GradeId == gradeId
                                      && sa.IsActive
                                      && _context.SubjectAssignments.Any(suj =>
                                          suj.SubjectId == subjectId
                                          && suj.GroupId == groupId
                                          && suj.GradeLevelId == gradeId
                                          && suj.SchoolId == schoolId)
                                orderby student.LastName, student.Name
                                select new StudentBasicDto
                                {
                                    StudentId = student.Id,
                                    FullName = $"{student.LastName}, {student.Name}",  // Apellido, Nombre
                                    GradeName = grade.Name,
                                    GroupName = grupo.Name,
                                    DocumentId = student.DocumentId ?? ""
                                }).ToListAsync();

            return result;
        }
    }
}
