namespace dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

/// <summary>Policies and shift schedules held in dictionaries.</summary>
public class InMemoryDepartmentRepository : IDepartmentRepository
{
    private readonly Dictionary<UserId, DepartmentPolicy> _policies = new();
    private readonly Dictionary<UserId, List<Shift>> _shifts = new();

    public void SetPolicy(UserId agentId, DepartmentPolicy policy) => _policies[agentId] = policy;

    public void AddShift(UserId agentId, Shift shift)
    {
        if (!_shifts.TryGetValue(agentId, out var list)) _shifts[agentId] = list = new List<Shift>();
        list.Add(shift);
    }

    public DepartmentPolicy GetDepartmentPolicy(UserId agentId) =>
        _policies.TryGetValue(agentId, out var policy)
            ? policy
            : throw new ArgumentException($"No department for agent {agentId}");

    public IReadOnlyList<Shift> GetUpcomingShifts(UserId agentId, DateTimeOffset from, DateTimeOffset to) =>
        _shifts.TryGetValue(agentId, out var list)
            ? list.Where(s => s.End > from && s.Start < to).ToList()
            : [];
}
