using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class ActivityAttachment
{
    public Guid Id { get; set; }

    public Guid ActivityId { get; set; }

    public string FileName { get; set; } = null!;

    public string StoragePath { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public DateTime UploadedAt { get; set; }

    public virtual Activity Activity { get; set; } = null!;
}
