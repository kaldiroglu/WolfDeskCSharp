using dev.kaldiroglu.WolfDesk.Ticketing;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing;

public class TicketTests
{
    private static readonly DateTimeOffset Opened = DateTimeOffset.Parse("2026-10-28T20:00:00Z");
    private static readonly DateTimeOffset Deadline = DateTimeOffset.Parse("2026-10-28T22:00:00Z");

    private readonly UserId _customer = UserId.Random();
    private readonly UserId _agent = UserId.Random();
    private readonly UserId _otherAgent = UserId.Random();
    private readonly Ticket _ticket;

    public TicketTests()
    {
        _ticket = Ticket.Open(TicketId.Random(), _customer, [ProductId.Random()], "Cannot log in",
            Priority.High, Opened);
        _ticket.Execute(new AssignAgent(_agent, new ResponseTimeframe(Opened, Deadline), Opened));
        _ticket.PullDomainEvents();
    }

    [Fact]
    public void OpeningATicketRecordsTicketOpened()
    {
        var t = Ticket.Open(TicketId.Random(), _customer, [ProductId.Random()], "Cannot log in", Priority.Low, Opened);
        Assert.IsType<TicketOpened>(t.PullDomainEvents()[0]);
    }

    [Fact]
    public void ATicketIsAboutAtLeastOneProduct() =>
        Assert.Throws<TicketRuleViolationException>(() =>
            Ticket.Open(TicketId.Random(), _customer, [], "Cannot log in", Priority.Low, Opened));

    [Fact]
    public void AMessageFromTheCustomerGoesToTheAgent()
    {
        _ticket.Execute(new AddMessage(_customer, "Still broken", Opened));

        Assert.Equal(_agent, _ticket.Messages[0].To);
        Assert.Equal(1, _ticket.GetUnreadMessagesCount(_agent));
    }

    [Fact]
    public void OnlyTheCustomerOrTheAgentCanWrite() =>
        Assert.Throws<TicketRuleViolationException>(() => _ticket.Execute(new AddMessage(_otherAgent, "Hello", Opened)));

    [Fact]
    public void OnlyTheReceiverCanAcknowledgeAMessage()
    {
        _ticket.Execute(new AddMessage(_customer, "Still broken", Opened));
        var id = _ticket.Messages[0].Id;

        Assert.Throws<TicketRuleViolationException>(() => _ticket.Execute(new AcknowledgeMessage(id, _customer, Opened)));
        _ticket.Execute(new AcknowledgeMessage(id, _agent, Opened));
        Assert.Equal(0, _ticket.GetUnreadMessagesCount(_agent));
    }

    [Fact]
    public void AnEscalationCannotBeRequestedBeforeTheDeadline()
    {
        Assert.Throws<TicketRuleViolationException>(() =>
            _ticket.Execute(new RequestEscalation(_customer, Deadline.AddSeconds(-1))));
        Assert.False(_ticket.IsEscalated);
    }

    [Fact]
    public void AnEscalationRequestedAfterTheDeadlineEscalatesTheTicket()
    {
        _ticket.Execute(new RequestEscalation(_customer, Deadline));

        Assert.True(_ticket.IsEscalated);
        var escalated = Assert.IsType<TicketEscalated>(_ticket.PullDomainEvents()[0]);
        Assert.Equal(EscalationReason.CustomerRequest, escalated.Reason);
    }

    [Fact]
    public void ATicketIsEscalatedOnlyOnce()
    {
        _ticket.Execute(new RequestEscalation(_customer, Deadline));
        Assert.Throws<TicketRuleViolationException>(() => _ticket.Execute(new RequestEscalation(_customer, Deadline)));
    }

    [Fact]
    public void OnlyTheCustomerCanRequestAnEscalation() =>
        Assert.Throws<TicketRuleViolationException>(() => _ticket.Execute(new RequestEscalation(_agent, Deadline)));

    [Fact]
    public void AMissedDeadlineEscalatesTheTicketAutomatically()
    {
        _ticket.Execute(new EvaluateAutomaticActions(Deadline, _otherAgent));

        Assert.True(_ticket.IsEscalated);
        var escalated = Assert.IsType<TicketEscalated>(_ticket.PullDomainEvents()[0]);
        Assert.Equal(EscalationReason.MissedSla, escalated.Reason);
    }

    [Fact]
    public void AnEscalatedTicketWithUnreadMessagesIsReassigned()
    {
        _ticket.Execute(new AddMessage(_customer, "Anyone there?", Opened));
        _ticket.Execute(new RequestEscalation(_customer, Deadline));

        _ticket.Execute(new EvaluateAutomaticActions(Deadline, _otherAgent));

        Assert.Equal(_otherAgent, _ticket.AssignedAgent);
        Assert.Contains(_ticket.PullDomainEvents(), e => e is TicketReassigned);
    }

    [Fact]
    public void AnEscalatedTicketWhoseMessagesWereReadKeepsItsAgent()
    {
        _ticket.Execute(new AddMessage(_customer, "Anyone there?", Opened));
        _ticket.Execute(new AcknowledgeMessage(_ticket.Messages[0].Id, _agent, Opened));
        _ticket.Execute(new RequestEscalation(_customer, Deadline));

        _ticket.Execute(new EvaluateAutomaticActions(Deadline, _otherAgent));

        Assert.Equal(_agent, _ticket.AssignedAgent);
    }

    [Fact]
    public void AnEscalatedTicketWithMoreThanHalfItsTimeLeftKeepsItsAgent()
    {
        _ticket.Execute(new AddMessage(_customer, "Anyone there?", Opened));
        _ticket.Execute(new RequestEscalation(_customer, Deadline));
        _ticket.Execute(new AssignAgent(_agent, new ResponseTimeframe(Deadline, Deadline.AddSeconds(7200)), Deadline));

        _ticket.Execute(new EvaluateAutomaticActions(Deadline.AddSeconds(3000), _otherAgent));

        Assert.Equal(_agent, _ticket.AssignedAgent);
    }

    [Fact]
    public void AClosedTicketTakesNoMoreMessages()
    {
        _ticket.Execute(new CloseTicket(_customer, Opened));

        Assert.Equal(TicketStatus.Closed, _ticket.Status);
        Assert.Throws<TicketRuleViolationException>(() => _ticket.Execute(new AddMessage(_customer, "One more thing", Opened)));
    }

    [Fact]
    public void ARestoredTicketHasTheSameState()
    {
        _ticket.Execute(new AddMessage(_customer, "Still broken", Opened));

        var restored = Ticket.Restore(_ticket.Snapshot());

        Assert.Equal(_ticket.Snapshot(), restored.Snapshot());
    }
}
