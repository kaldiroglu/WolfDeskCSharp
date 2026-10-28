namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// A domain service (listing 06-18): stateless business logic that belongs to no single
/// aggregate. It reads the department's policy and the agent's shifts, and returns the time
/// the agent has to respond, counted in working time only.
/// </summary>
public sealed class ResponseTimeframeCalculationService(IDepartmentRepository departmentRepository)
{
    public ResponseTimeframe CalculateAgentResponseDeadline(UserId agentId, Priority priority,
        bool escalated, DateTimeOffset startTime)
    {
        var policy = departmentRepository.GetDepartmentPolicy(agentId);
        var maxProcTime = policy.GetMaxResponseTimeFor(priority);

        if (escalated)
        {
            maxProcTime = maxProcTime * policy.EscalationFactor;
        }

        var shifts = departmentRepository.GetUpcomingShifts(agentId,
            startTime, startTime.Add(policy.MaxAgentResponseTime));

        return CalculateTargetTime(startTime, maxProcTime, shifts);
    }

    /// <summary>Spends the working time shift by shift, from the start time on.</summary>
    private static ResponseTimeframe CalculateTargetTime(DateTimeOffset startTime, TimeSpan workingTime,
        IReadOnlyList<Shift> shifts)
    {
        var left = workingTime;
        foreach (var shift in shifts.OrderBy(s => s.Start))
        {
            var from = shift.Start > startTime ? shift.Start : startTime;
            if (from >= shift.End) continue;
            var available = shift.End - from;
            if (available >= left) return new ResponseTimeframe(startTime, from + left);
            left -= available;
        }
        throw new InvalidOperationException("The agent's shifts do not cover the response time");
    }
}
