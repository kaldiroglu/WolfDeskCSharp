using dev.kaldiroglu.WolfDesk.Ticketing.Application;
using dev.kaldiroglu.WolfDesk.Ticketing.Domain.Events;
using dev.kaldiroglu.WolfDesk.Ticketing.Domain;
using dev.kaldiroglu.WolfDesk.Ticketing.ReadModels;

namespace dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

/// <summary>
/// Mimics a tickets table with a version column. A save succeeds only if the stored version
/// is the one the ticket was loaded with, and then increments it, like listing 06-12:
/// <c>UPDATE tickets ... WHERE ticket_id=@id and agg_version=@expected_version</c>.
/// Each save also stamps the row with the next checkpoint, like SQL Server's
/// <c>rowversion</c> column (Figure 8-15), so the read models can catch up on the changes.
/// The ticket's new domain events go to an outbox table in the same save, so the ticket and
/// its events are committed together (chapter 9, p. 145).
/// </summary>
public class InMemoryTicketRepository : ITicketRepository, ITicketChangeFeed, IOutbox
{
    private readonly Dictionary<TicketId, TicketChange> _rows = new();
    private readonly SortedDictionary<long, DomainEvent> _outbox = new();
    private readonly Lock _lock = new();
    private long _lastCheckpoint;
    private long _lastOutboxPosition;

    public Ticket Load(TicketId id)
    {
        lock (_lock)
        {
            return _rows.TryGetValue(id, out var row) ? Ticket.Restore(row.Ticket) : throw new TicketNotFoundException(id);
        }
    }

    public void Save(Ticket ticket)
    {
        lock (_lock)
        {
            var snapshot = ticket.Snapshot();
            var storedVersion = _rows.TryGetValue(snapshot.Id, out var stored) ? stored.Ticket.Version : 0;
            if (storedVersion != snapshot.Version)
                throw new ConcurrencyException(snapshot.Id, snapshot.Version, storedVersion);
            _lastCheckpoint++;
            _rows[snapshot.Id] = new TicketChange(_lastCheckpoint, snapshot with { Version = storedVersion + 1 });
            foreach (var domainEvent in ticket.PullDomainEvents()) _outbox[++_lastOutboxPosition] = domainEvent;
        }
    }

    public IReadOnlyList<OutboxRecord> Unpublished()
    {
        lock (_lock)
        {
            return _outbox.Select(row => new OutboxRecord(row.Key, row.Value)).ToList();
        }
    }

    public void MarkPublished(long position)
    {
        lock (_lock)
        {
            _outbox.Remove(position);
        }
    }

    public IReadOnlyList<TicketChange> ChangesAfter(long checkpoint)
    {
        lock (_lock)
        {
            return _rows.Values
                .Where(row => row.Checkpoint > checkpoint)
                .OrderBy(row => row.Checkpoint)
                .ToList();
        }
    }
}
