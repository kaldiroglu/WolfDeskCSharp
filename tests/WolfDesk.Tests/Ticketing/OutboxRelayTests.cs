using dev.kaldiroglu.WolfDesk.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;
using dev.kaldiroglu.WolfDesk.Ticketing.PublishedLanguage;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing;

public class OutboxRelayTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-28T20:00:00Z");

    private readonly InMemoryTicketRepository _tickets = new();
    private readonly InMemoryEventPublisher _bus = new();
    private readonly UserId _customer = UserId.Random();
    private readonly ProductId _product = ProductId.Random();

    private Ticket OpenAssignAndClose()
    {
        var ticket = Ticket.Open(TicketId.Random(), _customer, [_product], "Cannot log in", Priority.High, Now);
        ticket.Execute(new AssignAgent(UserId.Random(), new ResponseTimeframe(Now, Now.AddHours(1)), Now));
        ticket.Execute(new CloseTicket(_customer, Now));
        _tickets.Save(ticket);
        return ticket;
    }

    /// <summary>Publishes through the given publisher, but fails on the given call.</summary>
    private sealed class FailingPublisher(IEventPublisher inner, int failingCall) : IEventPublisher
    {
        private int _calls;

        public void Publish(TicketingMessage message)
        {
            if (++_calls == failingCall) throw new InvalidOperationException("The message bus is down");
            inner.Publish(message);
        }
    }

    /// <summary>The relay publishes the first event, then fails before marking it as published.</summary>
    private sealed class StopsOnceOutbox(InMemoryTicketRepository inner) : IOutbox
    {
        private bool _failed;

        public IReadOnlyList<OutboxRecord> Unpublished() => inner.Unpublished();

        public void MarkPublished(long position)
        {
            if (!_failed)
            {
                _failed = true;
                throw new InvalidOperationException("The relay stopped");
            }
            inner.MarkPublished(position);
        }
    }

    [Fact]
    public void OnlyThePublicEventsArePublishedInThePublishedLanguage()
    {
        var ticket = OpenAssignAndClose();

        new OutboxRelay(_tickets, _bus).PublishPending();

        Assert.Equal([typeof(TicketOpenedMessage), typeof(TicketClosedMessage)], _bus.Published.Select(m => m.GetType()));
        var opened = Assert.IsType<TicketOpenedMessage>(_bus.Published[0]);
        Assert.Equal(ticket.Id.Value, opened.TicketId);
        Assert.Equal([_product.Value], opened.Products);
        Assert.Empty(_tickets.Unpublished());
    }

    [Fact]
    public void AnEventThatFailedToPublishStaysInTheOutboxForTheNextRun()
    {
        OpenAssignAndClose();
        var relay = new OutboxRelay(_tickets, new FailingPublisher(_bus, 1));

        Assert.Throws<InvalidOperationException>(relay.PublishPending);
        Assert.Equal(3, _tickets.Unpublished().Count);

        relay.PublishPending();
        Assert.Equal(2, _bus.Published.Count);
        Assert.Empty(_tickets.Unpublished());
    }

    [Fact]
    public void AnEventIsPublishedAgainIfTheRelayStopsBeforeMarkingIt()
    {
        OpenAssignAndClose();
        var relay = new OutboxRelay(new StopsOnceOutbox(_tickets), _bus);

        Assert.Throws<InvalidOperationException>(relay.PublishPending);
        relay.PublishPending();

        Assert.Equal([typeof(TicketOpenedMessage), typeof(TicketOpenedMessage), typeof(TicketClosedMessage)],
            _bus.Published.Select(m => m.GetType()));
    }
}
