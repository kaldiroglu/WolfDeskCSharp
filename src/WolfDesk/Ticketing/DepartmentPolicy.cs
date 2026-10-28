namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// A support department's response rules: the working time an agent has per priority, the
/// factor applied to it when a ticket is escalated, and how far ahead to look for shifts.
/// </summary>
public sealed record DepartmentPolicy(IReadOnlyDictionary<Priority, TimeSpan> MaxResponseTimes,
    double EscalationFactor, TimeSpan MaxAgentResponseTime)
{
    public TimeSpan GetMaxResponseTimeFor(Priority priority) =>
        MaxResponseTimes.TryGetValue(priority, out var time)
            ? time
            : throw new ArgumentException($"No response time for {priority}");
}
