namespace dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;

/// <summary>
/// A second projection of the ticket's events, for analysis (the book's
/// <c>AnalysisModelProjection</c>, p. 106): how many messages a ticket took, how often it
/// was reassigned, and how it ended. It was added without changing the ticket or its events,
/// and it sees each ticket's whole history.
/// </summary>
public sealed class TicketAnalysisProjection
{
    public TicketId? TicketId { get; private set; }
    public int Messages { get; private set; }
    public int Reassignments { get; private set; }
    public bool WasEscalated { get; private set; }
    public TicketStatus Status { get; private set; }
    public int Version { get; private set; } = -1;

    public static TicketAnalysisProjection Of(IEnumerable<TicketEvent> events)
    {
        var projection = new TicketAnalysisProjection();
        foreach (var e in events) projection.Apply(e);
        return projection;
    }

    public void Apply(TicketEvent @event)
    {
        switch (@event)
        {
            case TicketOpened e:
                TicketId = e.TicketId;
                Status = TicketStatus.Open;
                break;
            case MessageAdded:
                Messages += 1;
                break;
            case TicketEscalated:
                WasEscalated = true;
                break;
            case TicketReassigned:
                Reassignments += 1;
                break;
            case TicketClosed:
                Status = TicketStatus.Closed;
                break;
        }
        Version += 1;
    }
}
