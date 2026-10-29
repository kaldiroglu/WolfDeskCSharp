namespace dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;

/// <summary>
/// The ticket's domain events, its source of truth. Chapter 6's events tell the rest of the
/// system what happened; these must also be enough to rebuild the ticket, so each one carries
/// every value it changes: the title, the response timeframe, the message body.
/// </summary>
public abstract record TicketEvent(TicketId TicketId, DateTimeOffset OccurredAt);

/// <summary>A customer opened a ticket.</summary>
public sealed record TicketOpened(TicketId TicketId, UserId Customer, IReadOnlyList<ProductId> Products,
    string Title, Priority Priority, DateTimeOffset OccurredAt) : TicketEvent(TicketId, OccurredAt);

/// <summary>An agent was assigned, with the time the agent has to respond.</summary>
public sealed record TicketAssigned(TicketId TicketId, UserId Agent, ResponseTimeframe Timeframe,
    DateTimeOffset OccurredAt) : TicketEvent(TicketId, OccurredAt);

/// <summary>The customer or the agent added a message.</summary>
public sealed record MessageAdded(TicketId TicketId, MessageId MessageId, UserId From, UserId To,
    string Body, DateTimeOffset OccurredAt) : TicketEvent(TicketId, OccurredAt);

/// <summary>The receiver acknowledged a message.</summary>
public sealed record MessageRead(TicketId TicketId, MessageId MessageId, DateTimeOffset OccurredAt)
    : TicketEvent(TicketId, OccurredAt);

/// <summary>The ticket was escalated: the deadline passed, or the customer asked.</summary>
public sealed record TicketEscalated(TicketId TicketId, EscalationReason Reason, DateTimeOffset OccurredAt)
    : TicketEvent(TicketId, OccurredAt);

/// <summary>An escalated ticket went to another agent.</summary>
public sealed record TicketReassigned(TicketId TicketId, UserId PreviousAgent, UserId NewAgent,
    DateTimeOffset OccurredAt) : TicketEvent(TicketId, OccurredAt);

/// <summary>The customer or the agent closed the ticket.</summary>
public sealed record TicketClosed(TicketId TicketId, UserId ClosedBy, DateTimeOffset OccurredAt)
    : TicketEvent(TicketId, OccurredAt);
