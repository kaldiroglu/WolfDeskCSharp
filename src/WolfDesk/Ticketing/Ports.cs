namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// Loads and saves tickets, one per transaction. <see cref="Save"/> uses optimistic
/// concurrency: it fails with <see cref="ConcurrencyException"/> if the ticket changed since
/// it was loaded (listings 06-11 and 06-12). Load a ticket again before each change.
/// </summary>
public interface ITicketRepository
{
    Ticket Load(TicketId id);
    void Save(Ticket ticket);
}

/// <summary>Publishes a ticket's domain events to the rest of the system.</summary>
public interface IEventPublisher
{
    void Publish(DomainEvent domainEvent);
}

public interface IDepartmentRepository
{
    DepartmentPolicy GetDepartmentPolicy(UserId agentId);

    /// <summary>The agent's shifts that overlap the period from <paramref name="from"/> to <paramref name="to"/>.</summary>
    IReadOnlyList<Shift> GetUpcomingShifts(UserId agentId, DateTimeOffset from, DateTimeOffset to);
}
