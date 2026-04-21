using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class ScheduleEntry
{
    public Guid Id { get; set; }

    public Guid TeacherAssignmentId { get; set; }

    public Guid TimeSlotId { get; set; }

    /// <summary>Día de la semana: 1 = Lunes … 7 = Domingo.</summary>
    public byte DayOfWeek { get; set; }

    public Guid AcademicYearId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual AcademicYear AcademicYear { get; set; } = null!;

    public virtual TeacherAssignment TeacherAssignment { get; set; } = null!;

    public virtual TimeSlot TimeSlot { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }
}
