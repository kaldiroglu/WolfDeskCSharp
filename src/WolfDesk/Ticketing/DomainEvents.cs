namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>Something significant that has happened to a ticket. Named in the past tense.</summary>
public abstract record DomainEvent(TicketId TicketId, DateTimeOffset OccurredAt);

/// <summary>A customer opened a ticket.</summary>
public sealed record TicketOpened(TicketId TicketId, UserId Customer, IReadOnlyList<ProductId> Products,
    Priority Priority, DateTimeOffset OccurredAt) : DomainEvent(TicketId, OccurredAt);

/// <summary>An agent was assigned, with a deadline to respond.</summary>
public sealed record TicketAssigned(TicketId TicketId, UserId Agent, DateTimeOffset ResponseDeadline,
    DateTimeOffset OccurredAt) : DomainEvent(TicketId, OccurredAt);

/// <summary>The customer or the agent added a message.</summary>
public sealed record MessageAdded(TicketId TicketId, MessageId MessageId, UserId From, UserId To,
    DateTimeOffset OccurredAt) : DomainEvent(TicketId, OccurredAt);

/// <summary>The receiver acknowledged a message.</summary>
public sealed record MessageRead(TicketId TicketId, MessageId MessageId, DateTimeOffset OccurredAt)
    : DomainEvent(TicketId, OccurredAt);

/// <summary>The ticket was escalated: the deadline passed, or the customer asked (listing 06-16).</summary>
public sealed record TicketEscalated(TicketId TicketId, EscalationReason Reason, DateTimeOffset OccurredAt)
    : DomainEvent(TicketId, OccurredAt);

/// <summary>An escalated ticket went to another agent.</summary>
public sealed record TicketReassigned(TicketId TicketId, UserId PreviousAgent, UserId NewAgent,
    DateTimeOffset OccurredAt) : DomainEvent(TicketId, OccurredAt);

/// <summary>The customer or the agent closed the ticket.</summary>
public sealed record TicketClosed(TicketId TicketId, UserId ClosedBy, DateTimeOffset OccurredAt)
    : DomainEvent(TicketId, OccurredAt);
