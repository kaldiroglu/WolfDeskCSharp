namespace dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;

/// <summary>
/// The application service of the event-sourced ticket. Each operation follows the book's
/// script (p. 108): load the ticket's events, rehydrate the ticket, execute one command, and
/// append the new events at the version the ticket was loaded at.
/// </summary>
public sealed class TicketService(IEventStore eventStore,
    ResponseTimeframeCalculationService timeframes, TimeProvider clock)
{
    public TicketId Open(UserId customer, IReadOnlyList<ProductId> products, string title, Priority priority)
    {
        var ticket = Ticket.Open(TicketId.Random(), customer, products, title, priority, clock.GetUtcNow());
        eventStore.Append(ticket.Id, ticket.NewEvents, -1);
        return ticket.Id;
    }

    public void Assign(TicketId id, UserId agent)
    {
        var ticket = Load(id);
        var originalVersion = ticket.Version;
        var now = clock.GetUtcNow();
        var timeframe = timeframes.CalculateAgentResponseDeadline(agent, ticket.Priority, ticket.IsEscalated, now);
        ticket.Execute(new AssignAgent(agent, timeframe, now));
        eventStore.Append(id, ticket.NewEvents, originalVersion);
    }

    public MessageId AddMessage(TicketId id, UserId from, string body)
    {
        var ticket = Load(id);
        var originalVersion = ticket.Version;
        ticket.Execute(new AddMessage(from, body, clock.GetUtcNow()));
        eventStore.Append(id, ticket.NewEvents, originalVersion);
        return ticket.Messages[^1].Id;
    }

    public void Acknowledge(TicketId id, MessageId messageId, UserId by)
    {
        var ticket = Load(id);
        var originalVersion = ticket.Version;
        ticket.Execute(new AcknowledgeMessage(messageId, by, clock.GetUtcNow()));
        eventStore.Append(id, ticket.NewEvents, originalVersion);
    }

    /// <summary>The book's <c>TicketAPI.RequestEscalation</c>, p. 108; failures are reported, not thrown.</summary>
    public ExecutionResult Escalate(TicketId id, UserId requestedBy)
    {
        try
        {
            var events = eventStore.Fetch(id);
            var ticket = new Ticket(events);
            var originalVersion = ticket.Version;
            var cmd = new RequestEscalation(requestedBy, clock.GetUtcNow());
            ticket.Execute(cmd);
            eventStore.Append(id, ticket.NewEvents, originalVersion);
            return ExecutionResult.Succeeded();
        }
        catch (Exception ex) when (ex is ConcurrencyException or TicketRuleViolationException)
        {
            return ExecutionResult.Failed(ex);
        }
    }

    public void EvaluateAutomaticActions(TicketId id, UserId replacementAgent)
    {
        var ticket = Load(id);
        var originalVersion = ticket.Version;
        ticket.Execute(new EvaluateAutomaticActions(clock.GetUtcNow(), replacementAgent));
        eventStore.Append(id, ticket.NewEvents, originalVersion);
    }

    public void Close(TicketId id, UserId closedBy)
    {
        var ticket = Load(id);
        var originalVersion = ticket.Version;
        ticket.Execute(new CloseTicket(closedBy, clock.GetUtcNow()));
        eventStore.Append(id, ticket.NewEvents, originalVersion);
    }

    public Ticket Load(TicketId id)
    {
        var events = eventStore.Fetch(id);
        return events.Count == 0 ? throw new TicketNotFoundException(id) : new Ticket(events);
    }

    /// <summary>Time travel: the ticket as it was at <paramref name="version"/>, rebuilt from its first events only.</summary>
    public Ticket LoadAt(TicketId id, int version)
    {
        var events = eventStore.Fetch(id);
        if (version < 0 || version >= events.Count)
            throw new ArgumentOutOfRangeException(nameof(version), $"Ticket {id.Value} has no version {version}");
        return new Ticket(events.Take(version + 1));
    }
}
