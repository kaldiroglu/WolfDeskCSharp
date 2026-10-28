namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// The application service: each operation loads one ticket, executes one command on it,
/// saves it, and then publishes its domain events. It holds no business rules itself.
/// </summary>
public sealed class TicketService(ITicketRepository ticketRepository,
    ResponseTimeframeCalculationService timeframes, IEventPublisher publisher, TimeProvider clock)
{
    public TicketId Open(UserId customer, IReadOnlyList<ProductId> products, string title, Priority priority)
    {
        var ticket = Ticket.Open(TicketId.Random(), customer, products, title, priority, clock.GetUtcNow());
        Save(ticket);
        return ticket.Id;
    }

    public void Assign(TicketId id, UserId agent)
    {
        var ticket = ticketRepository.Load(id);
        var now = clock.GetUtcNow();
        var timeframe = timeframes.CalculateAgentResponseDeadline(agent, ticket.Priority, ticket.IsEscalated, now);
        ticket.Execute(new AssignAgent(agent, timeframe, now));
        Save(ticket);
    }

    public MessageId AddMessage(TicketId id, UserId from, string body)
    {
        var ticket = ticketRepository.Load(id);
        ticket.Execute(new AddMessage(from, body, clock.GetUtcNow()));
        Save(ticket);
        return ticket.Messages[^1].Id;
    }

    public void Acknowledge(TicketId id, MessageId messageId, UserId by)
    {
        var ticket = ticketRepository.Load(id);
        ticket.Execute(new AcknowledgeMessage(messageId, by, clock.GetUtcNow()));
        Save(ticket);
    }

    /// <summary>Listing 06-10: a concurrent change is reported back, not thrown.</summary>
    public ExecutionResult Escalate(TicketId id, UserId requestedBy)
    {
        try
        {
            var ticket = ticketRepository.Load(id);
            var cmd = new RequestEscalation(requestedBy, clock.GetUtcNow());
            ticket.Execute(cmd);
            Save(ticket);
            return ExecutionResult.Succeeded();
        }
        catch (Exception ex) when (ex is ConcurrencyException or TicketRuleViolationException)
        {
            return ExecutionResult.Failed(ex);
        }
    }

    public void EvaluateAutomaticActions(TicketId id, UserId replacementAgent)
    {
        var ticket = ticketRepository.Load(id);
        ticket.Execute(new EvaluateAutomaticActions(clock.GetUtcNow(), replacementAgent));
        Save(ticket);
    }

    public void Close(TicketId id, UserId closedBy)
    {
        var ticket = ticketRepository.Load(id);
        ticket.Execute(new CloseTicket(closedBy, clock.GetUtcNow()));
        Save(ticket);
    }

    /// <summary>The events are published after the save; chapter 9's outbox makes the pair reliable.</summary>
    private void Save(Ticket ticket)
    {
        ticketRepository.Save(ticket);
        foreach (var domainEvent in ticket.PullDomainEvents()) publisher.Publish(domainEvent);
    }
}
