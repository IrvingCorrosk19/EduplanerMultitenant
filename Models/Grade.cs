using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class Grade
{
    public Guid Id { get; set; }

    public Guid? ActivityId { get; set; }

    public Guid? StudentId { get; set; }

    public decimal? Value { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Activity? Activity { get; set; }

    public virtual Student? Student { get; set; }
}
