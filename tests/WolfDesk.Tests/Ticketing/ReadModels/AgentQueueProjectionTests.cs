using dev.kaldiroglu.WolfDesk.Tests.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;
using dev.kaldiroglu.WolfDesk.Ticketing.ReadModels;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing.ReadModels;

public class AgentQueueProjectionTests
{
    private static readonly DateTimeOffset Opened = DateTimeOffset.Parse("2026-10-28T10:00:00Z");

    private readonly UserId _customer = UserId.Random();
    private readonly UserId _agent = UserId.Random();
    private readonly UserId _otherAgent = UserId.Random();
    private readonly InMemoryTicketRepository _tickets = new();
    private readonly ResponseTimeframeCalculationService _timeframes;
    private readonly AgentQueueProjection _projection;

    public AgentQueueProjectionTests()
    {
        var departments = new InMemoryDepartmentRepository();
        departments.SetPolicy(_agent, new DepartmentPolicy(
            new Dictionary<Priority, TimeSpan> { [Priority.High] = TimeSpan.FromHours(2), [Priority.Low] = TimeSpan.FromHours(4) },
            0.5, TimeSpan.FromDays(1)));
        departments.AddShift(_agent, new Shift(DateTimeOffset.Parse("2026-10-28T09:00:00Z"), DateTimeOffset.Parse("2026-10-28T17:00:00Z")));
        _timeframes = new ResponseTimeframeCalculationService(departments);
        _projection = new AgentQueueProjection(_tickets);
    }

    private TicketService ServiceAt(DateTimeOffset now) =>
        new(_tickets, _timeframes, new InMemoryEventPublisher(), new FixedTimeProvider(now));

    private TicketId OpenAndAssign(string title, Priority priority)
    {
        var service = ServiceAt(Opened);
        var id = service.Open(_customer, [ProductId.Random()], title, priority);
        service.Assign(id, _agent);
        return id;
    }

    private IEnumerable<TicketId> QueueOf(UserId user) => _projection.OpenTicketsOf(user).Select(t => t.Id);

    [Fact]
    public void AnAssignedTicketAppearsInItsAgentsQueue()
    {
        var id = OpenAndAssign("Cannot log in", Priority.High);

        _projection.CatchUp();

        Assert.Equal(new QueuedTicket(id, "Cannot log in", Priority.High,
            DateTimeOffset.Parse("2026-10-28T12:00:00Z"), false), _projection.OpenTicketsOf(_agent)[0]);
    }

    [Fact]
    public void TheQueueListsTheEarliestDeadlineFirst()
    {
        var low = OpenAndAssign("Typo on the invoice", Priority.Low);
        var high = OpenAndAssign("Cannot log in", Priority.High);

        _projection.CatchUp();

        Assert.Equal([high, low], QueueOf(_agent));
    }

    [Fact]
    public void TheQueueChangesOnlyWhenTheProjectionCatchesUp()
    {
        var id = OpenAndAssign("Cannot log in", Priority.High);
        _projection.CatchUp();

        ServiceAt(Opened).Close(id, _customer);

        Assert.Equal([id], QueueOf(_agent));
        _projection.CatchUp();
        Assert.Empty(QueueOf(_agent));
    }

    [Fact]
    public void ACatchUpReadsOnlyTheChangesAfterItsCheckpoint()
    {
        OpenAndAssign("Cannot log in", Priority.High);
        OpenAndAssign("Typo on the invoice", Priority.Low);

        Assert.Equal(2, _projection.CatchUp());
        Assert.Equal(4, _projection.Checkpoint);
        Assert.Equal(0, _projection.CatchUp());
    }

    [Fact]
    public void AClosedTicketLeavesTheQueue()
    {
        var closed = OpenAndAssign("Cannot log in", Priority.High);
        var open = OpenAndAssign("Typo on the invoice", Priority.Low);
        ServiceAt(Opened).Close(closed, _agent);

        _projection.CatchUp();

        Assert.Equal([open], QueueOf(_agent));
    }

    [Fact]
    public void AReassignedTicketMovesToTheNewAgentsQueue()
    {
        var id = OpenAndAssign("Cannot log in", Priority.High);
        ServiceAt(Opened).AddMessage(id, _customer, "Still locked out");
        _projection.CatchUp();

        ServiceAt(DateTimeOffset.Parse("2026-10-28T12:00:00Z")).EvaluateAutomaticActions(id, _otherAgent);
        _projection.CatchUp();

        Assert.Empty(QueueOf(_agent));
        Assert.Equal([id], QueueOf(_otherAgent));
        Assert.True(_projection.OpenTicketsOf(_otherAgent)[0].Escalated);
    }

    [Fact]
    public void AResetProjectionRebuildsTheSameQueues()
    {
        OpenAndAssign("Cannot log in", Priority.High);
        OpenAndAssign("Typo on the invoice", Priority.Low);
        _projection.CatchUp();
        var before = _projection.OpenTicketsOf(_agent);

        _projection.Reset();
        Assert.Empty(_projection.OpenTicketsOf(_agent));
        _projection.CatchUp();

        Assert.Equal(before, _projection.OpenTicketsOf(_agent));
    }

    [Fact]
    public void AProjectionAddedLaterSeesEveryTicket()
    {
        var first = OpenAndAssign("Cannot log in", Priority.High);
        _projection.CatchUp();
        var second = OpenAndAssign("Typo on the invoice", Priority.Low);

        var later = new AgentQueueProjection(_tickets);
        later.CatchUp();

        Assert.Equal([first, second], later.OpenTicketsOf(_agent).Select(t => t.Id));
    }
}
