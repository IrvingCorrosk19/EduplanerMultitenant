using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class Group
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public string Name { get; set; } = null!;

    public string? Grade { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? Description { get; set; }
    
    public int? MaxCapacity { get; set; }
    
    public string? Shift { get; set; } // Jornada: Mañana, Tarde, Noche (mantener por compatibilidad)
    
    public Guid? ShiftId { get; set; } // Referencia a la tabla de jornadas

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    
    public virtual ICollection<Prematriculation> Prematriculations { get; set; } = new List<Prematriculation>();

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<DisciplineReport> DisciplineReports { get; set; } = new List<DisciplineReport>();

    public virtual ICollection<OrientationReport> OrientationReports { get; set; } = new List<OrientationReport>();

    public virtual School? School { get; set; }

    public virtual Shift? ShiftNavigation { get; set; }

    public virtual ICollection<StudentAssignment> StudentAssignments { get; set; } = new List<StudentAssignment>();

    public virtual ICollection<SubjectAssignment> SubjectAssignments { get; set; } = new List<SubjectAssignment>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual User? CreatedByUser { get; set; }

    public virtual User? UpdatedByUser { get; set; }
}
