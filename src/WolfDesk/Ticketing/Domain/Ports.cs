using dev.kaldiroglu.WolfDesk.Ticketing.Domain.Events;

namespace dev.kaldiroglu.WolfDesk.Ticketing.Domain;

/// <summary>
/// Loads and saves tickets, one per transaction. <see cref="Save"/> uses optimistic
/// concurrency: it fails with <see cref="ConcurrencyException"/> if the ticket changed since
/// it was loaded (listings 06-11 and 06-12). Load a ticket again before each change.
/// <see cref="Save"/> also writes the ticket's new domain events to the <see cref="IOutbox"/>,
/// in the same transaction: both are saved, or neither is (chapter 9, p. 145).
/// </summary>
public interface ITicketRepository
{
    Ticket Load(TicketId id);
    void Save(Ticket ticket);
}

/// <summary>One row of the outbox table: a domain event, at its position in the outbox.</summary>
public sealed record OutboxRecord(long Position, DomainEvent Event);

/// <summary>
/// The outbox (chapter 9, p. 145): the domain events saved in the same transaction as their
/// ticket, kept until the relay has published them.
/// </summary>
public interface IOutbox
{
    /// <summary>The events not yet published, oldest first.</summary>
    IReadOnlyList<OutboxRecord> Unpublished();

    void MarkPublished(long position);
}
