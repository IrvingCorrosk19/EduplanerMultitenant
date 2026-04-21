using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class SecuritySetting
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public int? PasswordMinLength { get; set; }

    public bool? RequireUppercase { get; set; }

    public bool? RequireLowercase { get; set; }

    public bool? RequireNumbers { get; set; }

    public bool? RequireSpecial { get; set; }

    public int? ExpiryDays { get; set; }

    public int? PreventReuse { get; set; }

    public int? MaxLoginAttempts { get; set; }

    public int? SessionTimeoutMinutes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual School? School { get; set; }
}
