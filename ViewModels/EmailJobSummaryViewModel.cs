namespace SchoolManager.ViewModels;

public sealed class EmailJobSummaryViewModel
{
    public int JobsToday     { get; init; }
    public int SentToday     { get; init; }
    public int FailedToday   { get; init; }
    public int PendingNow    { get; init; }
    public int DeadLetterNow { get; init; }

    /// <summary>Tasa de éxito de hoy (0–100). -1 si no hay ítems.</summary>
    public int SuccessRatePct => (SentToday + FailedToday) == 0
        ? -1
        : (int)Math.Round(SentToday * 100.0 / (SentToday + FailedToday));
}
