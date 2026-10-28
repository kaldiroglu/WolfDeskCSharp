namespace dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

/// <summary>
/// Mimics a tickets table with a version column. A save succeeds only if the stored version
/// is the one the ticket was loaded with, and then increments it, like listing 06-12:
/// <c>UPDATE tickets ... WHERE ticket_id=@id and agg_version=@expected_version</c>.
/// </summary>
public class InMemoryTicketRepository : ITicketRepository
{
    private readonly Dictionary<TicketId, TicketSnapshot> _rows = new();
    private readonly Lock _lock = new();

    public Ticket Load(TicketId id)
    {
        lock (_lock)
        {
            return _rows.TryGetValue(id, out var row) ? Ticket.Restore(row) : throw new TicketNotFoundException(id);
        }
    }

    public void Save(Ticket ticket)
    {
        lock (_lock)
        {
            var snapshot = ticket.Snapshot();
            var storedVersion = _rows.TryGetValue(snapshot.Id, out var stored) ? stored.Version : 0;
            if (storedVersion != snapshot.Version)
                throw new ConcurrencyException(snapshot.Id, snapshot.Version, storedVersion);
            _rows[snapshot.Id] = snapshot with { Version = storedVersion + 1 };
        }
    }
}
