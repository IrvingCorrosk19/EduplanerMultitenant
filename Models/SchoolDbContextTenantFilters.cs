using Microsoft.EntityFrameworkCore;
using SchoolManager.Infrastructure;

namespace SchoolManager.Models;

public partial class SchoolDbContext
{
    private Guid? _tenantId;
    private bool _isSuperAdmin;

    public SchoolDbContext(DbContextOptions<SchoolDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantId = tenantProvider.SchoolId;
        _isSuperAdmin = tenantProvider.IsSuperAdmin;
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Bypass solo para superadmin explícito. Si school_id está ausente pero el rol
        // no es superadmin (cookie vieja, claim faltante), el filtro impone SchoolId == null
        // que no coincidirá con ningún registro → devuelve vacío, nunca datos ajenos.
        // Individual services still carry explicit WHERE clauses for defense-in-depth.

        modelBuilder.Entity<User>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Student>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Group>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<GradeLevel>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Subject>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Specialty>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Activity>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<ActivityType>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Attendance>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<DisciplineReport>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<OrientationReport>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Trimester>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<SubjectAssignment>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<TeacherWorkPlan>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Payment>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<PaymentConcept>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Prematriculation>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<PrematriculationPeriod>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<CounselorAssignment>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<AcademicYear>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<SecuritySetting>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<EmailConfiguration>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Message>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<Shift>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<StudentActivityScore>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<SchoolIdCardSetting>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<TimeSlot>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<SchoolScheduleConfiguration>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<EmailJob>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);

        modelBuilder.Entity<StudentPaymentAccess>()
            .HasQueryFilter(e => (_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId);
    }
}
