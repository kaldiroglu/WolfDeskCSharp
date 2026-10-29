using dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;

namespace dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

/// <summary>
/// An append-only event store in memory: each ticket's events in the order they were
/// appended. An append succeeds only if the ticket's last event is the expected version,
/// the version the new events were decided on.
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<TicketId, List<TicketEvent>> _streams = new();
    private readonly Lock _lock = new();

    public IReadOnlyList<TicketEvent> Fetch(TicketId id)
    {
        lock (_lock)
        {
            return _streams.TryGetValue(id, out var stream) ? stream.ToList() : [];
        }
    }

    public void Append(TicketId id, IReadOnlyList<TicketEvent> newEvents, int expectedVersion)
    {
        lock (_lock)
        {
            var actualVersion = _streams.TryGetValue(id, out var stream) ? stream.Count - 1 : -1;
            if (actualVersion != expectedVersion)
                throw new ConcurrencyException(id, expectedVersion, actualVersion);
            if (stream is null) _streams[id] = stream = new List<TicketEvent>();
            stream.AddRange(newEvents);
        }
    }
}
