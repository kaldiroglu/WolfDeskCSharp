namespace dev.kaldiroglu.WolfDesk.Ticketing.Domain;

public interface IDepartmentRepository
{
    DepartmentPolicy GetDepartmentPolicy(UserId agentId);

    /// <summary>The agent's shifts that overlap the period from <paramref name="from"/> to <paramref name="to"/>.</summary>
    IReadOnlyList<Shift> GetUpcomingShifts(UserId agentId, DateTimeOffset from, DateTimeOffset to);
}
