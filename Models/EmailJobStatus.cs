namespace SchoolManager.Models;

/// <summary>Estados de un lote de envío masivo (email_jobs).</summary>
public static class EmailJobStatus
{
    public const string Created = "Created";
    public const string Accepted = "Accepted";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string CompletedWithErrors = "CompletedWithErrors";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}
