using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolManager.Application.Interfaces;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;

namespace SchoolManager.Infrastructure.Services
{
    public class AcademicAssignmentService : IAcademicAssignmentService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public AcademicAssignmentService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> AssignTeacherAsync(Guid teacherId, Guid subjectAssignmentId)
        {
            if (teacherId == Guid.Empty || subjectAssignmentId == Guid.Empty)
                throw new ArgumentException("Los IDs de docente y asignación académica no pueden estar vacíos.");

            try
            {
                var exists = await _context.TeacherAssignments.AnyAsync(a =>
                    a.TeacherId == teacherId &&
                    a.SubjectAssignmentId == subjectAssignmentId);

                if (exists)
                    return false;

                var assignment = new TeacherAssignment
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId,
                    SubjectAssignmentId = subjectAssignmentId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.TeacherAssignments.AddAsync(assignment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException ex)
            {
                // Posible error de restricción de clave foránea o única
                throw new InvalidOperationException("Error al guardar la asignación. Verifica que los datos sean válidos.", ex);
            }
            catch (Exception ex)
            {
                // Error genérico
                throw new Exception("Ocurrió un error inesperado al asignar al docente.", ex);
            }
        }

        public async Task<List<TeacherAssignmentRequest>> GetAssignmentsByTeacherAsync(Guid teacherId)
        {
            var groupedAssignments = await _context.TeacherAssignments
                .Where(a => a.TeacherId == teacherId)
                .Include(a => a.SubjectAssignment)
                .GroupBy(a => new
                {
                    a.SubjectAssignment.SubjectId,
                    a.SubjectAssignment.GradeLevelId
                })
                .Select(g => new TeacherAssignmentRequest
                {
                    UserId = teacherId,
                    SubjectId = g.Key.SubjectId,
                    GradeId = g.Key.GradeLevelId,
                    GroupIds = g.Select(x => x.SubjectAssignment.GroupId).ToList()
                })
                .ToListAsync();

            return groupedAssignments;
        }

        public async Task<bool> ExisteAsignacionAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId, Guid? schoolId)
        {
            return await _context.SubjectAssignments.AnyAsync(sa =>
                sa.SpecialtyId == specialtyId &&
                sa.AreaId == areaId &&
                sa.SubjectId == subjectId &&
                sa.GradeLevelId == gradeLevelId &&
                sa.GroupId == groupId &&
                sa.SchoolId == schoolId
            );
        }

        public async Task CreateAsignacionAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId, Guid? schoolId)
        {
            try
            {
                var asignacion = new SubjectAssignment
                {
                    Id = Guid.NewGuid(),
                    SpecialtyId = specialtyId,
                    SchoolId = schoolId,
                    AreaId = areaId,
                    SubjectId = subjectId,
                    GradeLevelId = gradeLevelId,
                    GroupId = groupId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubjectAssignments.Add(asignacion);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                var constraint = ((PostgresException)ex.InnerException).ConstraintName;
                throw new Exception($"Violación de llave única. Restricción duplicada: {constraint}", ex);
            }
        }


        public async Task<Guid?> GetSubjectAssignmentIdAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId, Guid? schoolId)
        {
            return await _context.SubjectAssignments
                .Where(sa =>
                    sa.SpecialtyId == specialtyId &&
                    sa.AreaId == areaId &&
                    sa.SubjectId == subjectId &&
                    sa.GradeLevelId == gradeLevelId &&
                    sa.GroupId == groupId &&
                    sa.SchoolId == schoolId)
                .Select(sa => (Guid?)sa.Id)
                .FirstOrDefaultAsync();
        }
    }
}
