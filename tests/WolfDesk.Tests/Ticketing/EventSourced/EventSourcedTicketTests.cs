using dev.kaldiroglu.WolfDesk.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;
using MessageAdded = dev.kaldiroglu.WolfDesk.Ticketing.EventSourced.MessageAdded;
using Ticket = dev.kaldiroglu.WolfDesk.Ticketing.EventSourced.Ticket;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing.EventSourced;

public class EventSourcedTicketTests
{
    private static readonly DateTimeOffset Opened = DateTimeOffset.Parse("2026-10-28T20:00:00Z");
    private static readonly DateTimeOffset Deadline = DateTimeOffset.Parse("2026-10-28T22:00:00Z");

    private readonly UserId _customer = UserId.Random();
    private readonly UserId _agent = UserId.Random();
    private readonly UserId _otherAgent = UserId.Random();
    private readonly Ticket _ticket;

    public EventSourcedTicketTests()
    {
        _ticket = Ticket.Open(TicketId.Random(), _customer, [ProductId.Random()], "Cannot log in", Priority.High, Opened);
        _ticket.Execute(new AssignAgent(_agent, new ResponseTimeframe(Opened, Deadline), Opened));
    }

    private static IEnumerable<string> TypesOf(IEnumerable<TicketEvent> events) => events.Select(e => e.GetType().Name);

    [Fact]
    public void ACommandAppendsAnEventInsteadOfChangingAField()
    {
        _ticket.Execute(new AddMessage(_customer, "Still broken", Opened));

        Assert.Equal(["TicketOpened", "TicketAssigned", "MessageAdded"], TypesOf(_ticket.NewEvents));
        var added = Assert.IsType<MessageAdded>(_ticket.NewEvents[^1]);
        Assert.Equal("Still broken", added.Body);
        Assert.Equal(_agent, added.To);
    }

    [Fact]
    public void ATicketIsRebuiltFromItsEvents()
    {
        _ticket.Execute(new AddMessage(_customer, "Still broken", Opened));
        _ticket.Execute(new RequestEscalation(_customer, Deadline));

        var rebuilt = new Ticket(_ticket.NewEvents);

        Assert.Equal(_ticket.Id, rebuilt.Id);
        Assert.Equal("Cannot log in", rebuilt.Title);
        Assert.Equal(_agent, rebuilt.AssignedAgent);
        Assert.True(rebuilt.IsEscalated);
        Assert.Equal("Still broken", rebuilt.Messages[0].Body);
        Assert.Equal(1, rebuilt.GetUnreadMessagesCount(_agent));
        Assert.Empty(rebuilt.NewEvents);
    }

    [Fact]
    public void TheVersionIsTheNumberOfTheLastEvent()
    {
        Assert.Equal(-1, new Ticket([]).Version);
        Assert.Equal(0, new Ticket(_ticket.NewEvents.Take(1)).Version);
        Assert.Equal(1, _ticket.Version);
    }

    [Fact]
    public void AnEarlierStateIsProjectedFromTheFirstEvents()
    {
        _ticket.Execute(new AddMessage(_customer, "Still broken", Opened));

        var asOpened = new Ticket(_ticket.NewEvents.Take(1));

        Assert.Null(asOpened.AssignedAgent);
        Assert.Empty(asOpened.Messages);
        Assert.Single(_ticket.Messages);
    }

    [Fact]
    public void ARejectedCommandAppendsNoEvent()
    {
        var before = _ticket.NewEvents;

        Assert.Throws<TicketRuleViolationException>(() => _ticket.Execute(new RequestEscalation(_customer, Opened)));
        Assert.Equal(before, _ticket.NewEvents);
    }

    [Fact]
    public void ARebuiltTicketKeepsItsRules()
    {
        _ticket.Execute(new RequestEscalation(_customer, Deadline));
        var rebuilt = new Ticket(_ticket.NewEvents);

        var ex = Assert.Throws<TicketRuleViolationException>(
            () => rebuilt.Execute(new RequestEscalation(_customer, Deadline)));
        Assert.Equal("The ticket is already escalated", ex.Message);
    }

    [Fact]
    public void AnEscalatedTicketWithUnreadMessagesIsReassigned()
    {
        _ticket.Execute(new AddMessage(_customer, "Still broken", Opened));

        _ticket.Execute(new EvaluateAutomaticActions(Deadline, _otherAgent));

        Assert.Equal(["TicketEscalated", "TicketReassigned"], TypesOf(_ticket.NewEvents.Skip(3)));
        Assert.Equal(_otherAgent, _ticket.AssignedAgent);
    }

    [Fact]
    public void AClosedTicketTakesNoMoreCommands()
    {
        _ticket.Execute(new CloseTicket(_customer, Opened));

        Assert.Equal(TicketStatus.Closed, _ticket.Status);
        Assert.Throws<TicketRuleViolationException>(
            () => _ticket.Execute(new AddMessage(_customer, "One more thing", Opened)));
    }
}
