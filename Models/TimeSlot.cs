using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class TimeSlot
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public Guid? ShiftId { get; set; }

    public string Name { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public virtual School School { get; set; } = null!;

    public virtual Shift? Shift { get; set; }

    public virtual ICollection<ScheduleEntry> ScheduleEntries { get; set; } = new List<ScheduleEntry>();
}
