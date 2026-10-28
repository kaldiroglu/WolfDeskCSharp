namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// The ticket aggregate. Its state changes only through its commands; each command checks
/// the ticket's rules first, then changes the state and records a domain event.
/// Other aggregates, the customer, the agent and the products, are referenced by id only.
/// The ticket's messages belong to it: no one changes a message except through its ticket.
/// </summary>
public sealed class Ticket
{
    private readonly List<Message> _messages;
    private readonly List<DomainEvent> _domainEvents = new();
    private UserId? _assignedAgent;
    private ResponseTimeframe? _responseTimeframe;

    private Ticket(TicketId id, int version, UserId customer, IReadOnlyList<ProductId> products,
        string title, Priority priority, UserId? assignedAgent, ResponseTimeframe? responseTimeframe,
        bool escalated, TicketStatus status, List<Message> messages)
    {
        Id = id;
        Version = version;
        Customer = customer;
        Products = products;
        Title = title;
        Priority = priority;
        _assignedAgent = assignedAgent;
        _responseTimeframe = responseTimeframe;
        IsEscalated = escalated;
        Status = status;
        _messages = messages;
    }

    public static Ticket Open(TicketId id, UserId customer, IReadOnlyList<ProductId> products,
        string title, Priority priority, DateTimeOffset at)
    {
        if (products.Count == 0) throw new TicketRuleViolationException("A ticket is about at least one product");
        if (string.IsNullOrWhiteSpace(title)) throw new TicketRuleViolationException("A ticket needs a title");
        var ticket = new Ticket(id, 0, customer, products.ToList(), title.Trim(), priority,
            null, null, false, TicketStatus.Open, new List<Message>());
        ticket._domainEvents.Add(new TicketOpened(id, customer, ticket.Products, priority, at));
        return ticket;
    }

    public TicketId Id { get; }
    public int Version { get; }
    public UserId Customer { get; }
    public IReadOnlyList<ProductId> Products { get; }
    public string Title { get; }
    public Priority Priority { get; }
    public UserId? AssignedAgent => _assignedAgent;
    public bool IsEscalated { get; private set; }
    public TicketStatus Status { get; private set; }
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    public void Execute(AssignAgent cmd)
    {
        MustBeOpen();
        _assignedAgent = cmd.Agent;
        _responseTimeframe = cmd.Timeframe;
        _domainEvents.Add(new TicketAssigned(Id, cmd.Agent, cmd.Timeframe.Deadline, cmd.At));
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
        var message = new Message(MessageId.Random(), cmd.From, to, cmd.Body, cmd.At, false);
        _messages.Add(message);
        _domainEvents.Add(new MessageAdded(Id, message.Id, cmd.From, to, cmd.At));
    }

    public void Execute(AcknowledgeMessage cmd)
    {
        MustBeOpen();
        var message = _messages.FirstOrDefault(x => x.Id == cmd.MessageId)
            ?? throw new TicketRuleViolationException("No such message");
        if (message.To != cmd.By)
            throw new TicketRuleViolationException("Only the receiver can acknowledge a message");
        if (message.WasRead) return;
        message.MarkAsRead();
        _domainEvents.Add(new MessageRead(Id, message.Id, cmd.At));
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
        IsEscalated = true;
        _domainEvents.Add(new TicketEscalated(Id, EscalationReason.CustomerRequest, cmd.At));
    }

    public void Execute(EvaluateAutomaticActions cmd)
    {
        if (Status != TicketStatus.Open || _assignedAgent is null) return;
        var remaining = GetRemainingTimePercentage(cmd.At);

        if (!IsEscalated && remaining <= 0)
        {
            IsEscalated = true;
            _domainEvents.Add(new TicketEscalated(Id, EscalationReason.MissedSla, cmd.At));
        }

        if (IsEscalated && remaining < 0.5 && GetUnreadMessagesCount(_assignedAgent) > 0)
        {
            var previous = _assignedAgent;
            _assignedAgent = cmd.ReplacementAgent;
            _domainEvents.Add(new TicketReassigned(Id, previous, _assignedAgent, cmd.At));
        }
    }

    public void Execute(CloseTicket cmd)
    {
        MustBeOpen();
        if (cmd.ClosedBy != Customer && cmd.ClosedBy != _assignedAgent)
            throw new TicketRuleViolationException("Only the customer or the assigned agent can close");
        Status = TicketStatus.Closed;
        _domainEvents.Add(new TicketClosed(Id, cmd.ClosedBy, cmd.At));
    }

    public int GetUnreadMessagesCount(UserId id) => _messages.Count(x => x.To == id && !x.WasRead);

    public double GetRemainingTimePercentage(DateTimeOffset now) =>
        _responseTimeframe?.RemainingTimePercentage(now) ?? 1.0;

    /// <summary>The events recorded since the ticket was loaded; returns them once.</summary>
    public IReadOnlyList<DomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    private void MustBeOpen()
    {
        if (Status != TicketStatus.Open) throw new TicketRuleViolationException("The ticket is closed");
    }

    private UserId AssignedAgentOrFail() =>
        _assignedAgent ?? throw new TicketRuleViolationException("No agent is assigned yet");

    public TicketSnapshot Snapshot() => new(Id, Version, Customer, Products, Title, Priority,
        _assignedAgent, _responseTimeframe, IsEscalated, Status,
        _messages.Select(m => new TicketSnapshot.MessageSnapshot(m.Id, m.From, m.To, m.Body, m.SentAt, m.WasRead))
            .ToList());

    public static Ticket Restore(TicketSnapshot s) => new(s.Id, s.Version, s.Customer, s.Products.ToList(),
        s.Title, s.Priority, s.AssignedAgent, s.ResponseTimeframe, s.Escalated, s.Status,
        s.Messages.Select(m => new Message(m.Id, m.From, m.To, m.Body, m.SentAt, m.WasRead)).ToList());
}
