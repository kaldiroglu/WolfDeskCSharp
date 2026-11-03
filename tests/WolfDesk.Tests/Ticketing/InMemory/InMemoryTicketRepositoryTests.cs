using dev.kaldiroglu.WolfDesk.Ticketing.Domain.Commands;
using dev.kaldiroglu.WolfDesk.Ticketing.Domain.Events;
using dev.kaldiroglu.WolfDesk.Ticketing.Domain;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing.InMemory;

public class InMemoryTicketRepositoryTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-28T20:00:00Z");

    private readonly InMemoryTicketRepository _repository = new();

    private static Ticket NewTicket() =>
        Ticket.Open(TicketId.Random(), UserId.Random(), [ProductId.Random()], "Cannot log in", Priority.Low, Now);

    [Fact]
    public void EachSaveIncrementsTheVersion()
    {
        var ticket = NewTicket();
        _repository.Save(ticket);
        Assert.Equal(1, _repository.Load(ticket.Id).Version);

        _repository.Save(_repository.Load(ticket.Id));
        Assert.Equal(2, _repository.Load(ticket.Id).Version);
    }

    [Fact]
    public void ATicketChangedSinceItWasLoadedIsNotSaved()
    {
        var ticket = NewTicket();
        _repository.Save(ticket);
        var first = _repository.Load(ticket.Id);
        var second = _repository.Load(ticket.Id);

        _repository.Save(first);

        Assert.Throws<ConcurrencyException>(() => _repository.Save(second));
    }

    [Fact]
    public void EverySaveTakesTheNextCheckpoint()
    {
        _repository.Save(NewTicket());
        _repository.Save(NewTicket());

        Assert.Equal([1L, 2L], _repository.ChangesAfter(0).Select(c => c.Checkpoint));
    }

    [Fact]
    public void ATicketSavedTwiceIsOneChangeAtItsLatestCheckpoint()
    {
        var ticket = NewTicket();
        _repository.Save(ticket);
        _repository.Save(_repository.Load(ticket.Id));

        var change = Assert.Single(_repository.ChangesAfter(0));
        Assert.Equal(2L, change.Checkpoint);
        Assert.Empty(_repository.ChangesAfter(2));
    }

    [Fact]
    public void ASaveKeepsTheTicketsNewEventsInTheOutbox()
    {
        _repository.Save(NewTicket());

        var record = Assert.Single(_repository.Unpublished());
        Assert.IsType<TicketOpened>(record.Event);
    }

    [Fact]
    public void ARejectedSaveKeepsNoEventsInTheOutbox()
    {
        var customer = UserId.Random();
        var ticket = Ticket.Open(TicketId.Random(), customer, [ProductId.Random()], "Cannot log in", Priority.Low, Now);
        _repository.Save(ticket);
        var first = _repository.Load(ticket.Id);
        var second = _repository.Load(ticket.Id);
        _repository.Save(first);
        second.Execute(new CloseTicket(customer, Now));

        Assert.Throws<ConcurrencyException>(() => _repository.Save(second));
        Assert.Single(_repository.Unpublished());
    }

    [Fact]
    public void APublishedEventLeavesTheOutbox()
    {
        _repository.Save(NewTicket());
        _repository.Save(NewTicket());

        _repository.MarkPublished(_repository.Unpublished()[0].Position);

        Assert.Equal([2L], _repository.Unpublished().Select(r => r.Position));
    }

    [Fact]
    public void AnUnknownTicketIsNotFound() =>
        Assert.Throws<TicketNotFoundException>(() => _repository.Load(TicketId.Random()));
}
