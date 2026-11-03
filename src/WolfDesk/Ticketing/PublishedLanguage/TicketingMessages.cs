using dev.kaldiroglu.WolfDesk.Ticketing.Domain;

namespace dev.kaldiroglu.WolfDesk.Ticketing.PublishedLanguage;

// Ticketing's published language (chapter 9, p. 140): the public messages other bounded
// contexts may consume. They are not the ticket's domain events. They carry only plain
// values, so a consumer depends on this namespace and on nothing else in ticketing, and the
// ticket's model can change without breaking them. Private events, such as a message being
// read, stay inside.

/// <summary>A public message of ticketing's published language.</summary>
public abstract record TicketingMessage(Guid TicketId, DateTimeOffset OccurredAt);

/// <summary>A ticket was opened for the given products.</summary>
public sealed record TicketOpenedMessage(Guid TicketId, IReadOnlyList<Guid> Products, string Priority,
    DateTimeOffset OccurredAt) : TicketingMessage(TicketId, OccurredAt);

/// <summary>A ticket was escalated.</summary>
public sealed record TicketEscalatedMessage(Guid TicketId, DateTimeOffset OccurredAt)
    : TicketingMessage(TicketId, OccurredAt);

/// <summary>A ticket was closed.</summary>
public sealed record TicketClosedMessage(Guid TicketId, DateTimeOffset OccurredAt)
    : TicketingMessage(TicketId, OccurredAt);
