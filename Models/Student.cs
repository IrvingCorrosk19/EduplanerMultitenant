using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class Student
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly? BirthDate { get; set; }

    public string? Grade { get; set; }

    public string? GroupName { get; set; }

    public Guid? ParentId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Parent { get; set; }

    public virtual School? School { get; set; }
}
