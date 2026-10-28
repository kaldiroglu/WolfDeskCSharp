namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// The time an agent has to respond: from <c>Start</c> to <c>Deadline</c>. A value object
/// that validates itself and computes how much of the time is left.
/// </summary>
public sealed record ResponseTimeframe
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset Deadline { get; }

    public ResponseTimeframe(DateTimeOffset start, DateTimeOffset deadline)
    {
        if (deadline <= start) throw new ArgumentException("The deadline must come after the start");
        Start = start;
        Deadline = deadline;
    }

    /// <summary>1.0 at the start, 0.0 at the deadline, negative once the deadline has passed.</summary>
    public double RemainingTimePercentage(DateTimeOffset now) =>
        (Deadline - now).TotalMilliseconds / (Deadline - Start).TotalMilliseconds;
}
