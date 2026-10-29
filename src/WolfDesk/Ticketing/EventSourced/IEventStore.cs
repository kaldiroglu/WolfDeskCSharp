namespace dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;

/// <summary>
/// The event store (the book's <c>IEventStore</c>, p. 107). It is append-only: it fetches a
/// ticket's events and appends new ones, and offers no way to change or delete an event.
/// <see cref="Append"/> fails with <see cref="ConcurrencyException"/> if events were appended
/// after <paramref name="expectedVersion"/>, the version the new events are based on.
/// </summary>
public interface IEventStore
{
    IReadOnlyList<TicketEvent> Fetch(TicketId id);

    void Append(TicketId id, IReadOnlyList<TicketEvent> newEvents, int expectedVersion);
}
