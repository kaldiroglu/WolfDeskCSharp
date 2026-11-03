using dev.kaldiroglu.WolfDesk.Ticketing.PublishedLanguage;

namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// Translates the ticket's domain events into ticketing's published language: the
/// open-host side of a stateless model translation (chapter 9, pp. 138 and 140). Private
/// events have no public version. Every event is listed, and an unknown one throws, so a new
/// event fails until it is given a decision here.
/// </summary>
public static class PublishedLanguageTranslator
{
    public static TicketingMessage? Translate(DomainEvent domainEvent) => domainEvent switch
    {
        TicketOpened e => new TicketOpenedMessage(e.TicketId.Value,
            e.Products.Select(p => p.Value).ToList(), e.Priority.ToString(), e.OccurredAt),
        TicketEscalated e => new TicketEscalatedMessage(e.TicketId.Value, e.OccurredAt),
        TicketClosed e => new TicketClosedMessage(e.TicketId.Value, e.OccurredAt),
        TicketAssigned or MessageAdded or MessageRead or TicketReassigned => null,
        _ => throw new ArgumentOutOfRangeException(nameof(domainEvent), domainEvent.GetType().Name, "No translation"),
    };
}
