namespace dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;

/// <summary>
/// The ticket aggregate, event-sourced. It has chapter 6's commands and rules, but no stored
/// state: the constructor rebuilds the state from the ticket's events, and a command that
/// passes the rules appends a new event instead of changing a field.
/// </summary>
public sealed class Ticket
{
    private readonly TicketState _state = new();
    private readonly List<TicketEvent> _newEvents = new();

    /// <summary>Rehydrates the ticket: projects its events, oldest first, into its current state.</summary>
    public Ticket(IEnumerable<TicketEvent> events)
    {
        foreach (var e in events)
        {
            _state.Apply(e);
        }
    }

    public static Ticket Open(TicketId id, UserId customer, IReadOnlyList<ProductId> products,
        string title, Priority priority, DateTimeOffset at)
    {
        if (products.Count == 0) throw new TicketRuleViolationException("A ticket is about at least one product");
        if (string.IsNullOrWhiteSpace(title)) throw new TicketRuleViolationException("A ticket needs a title");
        var ticket = new Ticket([]);
        ticket.AppendEvent(new TicketOpened(id, customer, products.ToList(), title.Trim(), priority, at));
        return ticket;
    }

    public TicketId Id => _state.Id!;

    /// <summary>The number of the ticket's last event: 0 once opened, -1 for no events.</summary>
    public int Version => _state.Version;

    public UserId Customer => _state.Customer!;
    public string Title => _state.Title!;
    public Priority Priority => _state.Priority;
    public UserId? AssignedAgent => _state.AssignedAgent;
    public bool IsEscalated => _state.IsEscalated;
    public TicketStatus Status => _state.Status;
    public IReadOnlyList<TicketState.Message> Messages => _state.Messages;

    /// <summary>The events appended since the ticket was rehydrated, for the event store.</summary>
    public IReadOnlyList<TicketEvent> NewEvents => _newEvents.ToList();

    public void Execute(AssignAgent cmd)
    {
        MustBeOpen();
        AppendEvent(new TicketAssigned(Id, cmd.Agent, cmd.Timeframe, cmd.At));
    }

    public void Execute(AddMessage cmd)
    {
        MustBeOpen();
        var agent = AssignedAgentOrFail();
        UserId to;
        if (cmd.From == Customer)
            to = agent;
        else if (cmd.From == agent)
            to = Customer;
        else
            throw new TicketRuleViolationException("Only the customer or the assigned agent can write");
        if (string.IsNullOrWhiteSpace(cmd.Body)) throw new ArgumentException("A message needs a body");
        AppendEvent(new MessageAdded(Id, MessageId.Random(), cmd.From, to, cmd.Body, cmd.At));
    }

    public void Execute(AcknowledgeMessage cmd)
    {
        MustBeOpen();
        var message = _state.Messages.FirstOrDefault(x => x.Id == cmd.MessageId)
            ?? throw new TicketRuleViolationException("No such message");
        if (message.To != cmd.By)
            throw new TicketRuleViolationException("Only the receiver can acknowledge a message");
        if (message.WasRead) return;
        AppendEvent(new MessageRead(Id, message.Id, cmd.At));
    }

    public void Execute(RequestEscalation cmd)
    {
        MustBeOpen();
        if (cmd.RequestedBy != Customer)
            throw new TicketRuleViolationException("Only the customer can request an escalation");
        if (IsEscalated)
            throw new TicketRuleViolationException("The ticket is already escalated");
        if (GetRemainingTimePercentage(cmd.At) > 0)
            throw new TicketRuleViolationException("The response deadline has not passed yet");
        AppendEvent(new TicketEscalated(Id, EscalationReason.CustomerRequest, cmd.At));
    }

    public void Execute(EvaluateAutomaticActions cmd)
    {
        if (Status != TicketStatus.Open || AssignedAgent is not { } agent) return;
        var remaining = GetRemainingTimePercentage(cmd.At);

        if (!IsEscalated && remaining <= 0)
        {
            AppendEvent(new TicketEscalated(Id, EscalationReason.MissedSla, cmd.At));
        }

        if (IsEscalated && remaining < 0.5 && GetUnreadMessagesCount(agent) > 0)
        {
            AppendEvent(new TicketReassigned(Id, agent, cmd.ReplacementAgent, cmd.At));
        }
    }

    public void Execute(CloseTicket cmd)
    {
        MustBeOpen();
        if (cmd.ClosedBy != Customer && cmd.ClosedBy != AssignedAgent)
            throw new TicketRuleViolationException("Only the customer or the assigned agent can close");
        AppendEvent(new TicketClosed(Id, cmd.ClosedBy, cmd.At));
    }

    public int GetUnreadMessagesCount(UserId id) => _state.Messages.Count(x => x.To == id && !x.WasRead);

    public double GetRemainingTimePercentage(DateTimeOffset now) =>
        _state.ResponseTimeframe?.RemainingTimePercentage(now) ?? 1.0;

    /// <summary>A new event changes the state through the projection, the same way a loaded one does.</summary>
    private void AppendEvent(TicketEvent @event)
    {
        _state.Apply(@event);
        _newEvents.Add(@event);
    }

    private void MustBeOpen()
    {
        if (Status != TicketStatus.Open) throw new TicketRuleViolationException("The ticket is closed");
    }

    private UserId AssignedAgentOrFail() =>
        AssignedAgent ?? throw new TicketRuleViolationException("No agent is assigned yet");
}
