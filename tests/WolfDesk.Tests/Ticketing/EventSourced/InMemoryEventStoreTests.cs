using dev.kaldiroglu.WolfDesk.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;
using TicketClosed = dev.kaldiroglu.WolfDesk.Ticketing.EventSourced.TicketClosed;
using TicketOpened = dev.kaldiroglu.WolfDesk.Ticketing.EventSourced.TicketOpened;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing.EventSourced;

public class InMemoryEventStoreTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.Parse("2026-10-28T20:00:00Z");

    private readonly InMemoryEventStore _store = new();
    private readonly TicketId _id = TicketId.Random();
    private readonly UserId _customer = UserId.Random();
    private readonly TicketOpened _opened;
    private readonly TicketClosed _closed;

    public InMemoryEventStoreTests()
    {
        _opened = new TicketOpened(_id, _customer, [ProductId.Random()], "Cannot log in", Priority.High, At);
        _closed = new TicketClosed(_id, _customer, At);
    }

    [Fact]
    public void EventsAreFetchedInTheOrderTheyWereAppended()
    {
        _store.Append(_id, [_opened], -1);
        _store.Append(_id, [_closed], 0);

        Assert.Equal<TicketEvent>([_opened, _closed], _store.Fetch(_id));
    }

    [Fact]
    public void AnAppendBasedOnAStaleVersionFails()
    {
        _store.Append(_id, [_opened], -1);
        _store.Append(_id, [_closed], 0);

        Assert.Throws<ConcurrencyException>(() => _store.Append(_id, [new TicketClosed(_id, _customer, At)], 0));
        Assert.Equal(2, _store.Fetch(_id).Count);
    }

    [Fact]
    public void AnEarlierFetchIsNotChangedByLaterAppends()
    {
        _store.Append(_id, [_opened], -1);
        var fetched = _store.Fetch(_id);

        _store.Append(_id, [_closed], 0);

        Assert.Equal<TicketEvent>([_opened], fetched);
    }
}
