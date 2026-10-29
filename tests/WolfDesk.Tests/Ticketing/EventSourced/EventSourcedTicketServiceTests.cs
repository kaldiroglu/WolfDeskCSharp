using dev.kaldiroglu.WolfDesk.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;
using TicketAssigned = dev.kaldiroglu.WolfDesk.Ticketing.EventSourced.TicketAssigned;
using TicketClosed = dev.kaldiroglu.WolfDesk.Ticketing.EventSourced.TicketClosed;
using TicketService = dev.kaldiroglu.WolfDesk.Ticketing.EventSourced.TicketService;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing.EventSourced;

public class EventSourcedTicketServiceTests
{
    private static readonly DateTimeOffset Opened = DateTimeOffset.Parse("2026-10-28T10:00:00Z");
    private static readonly DateTimeOffset Deadline = DateTimeOffset.Parse("2026-10-28T12:00:00Z");

    private readonly UserId _customer = UserId.Random();
    private readonly UserId _agent = UserId.Random();
    private readonly UserId _otherAgent = UserId.Random();
    private readonly InMemoryEventStore _store = new();
    private readonly ResponseTimeframeCalculationService _timeframes;

    public EventSourcedTicketServiceTests()
    {
        var departments = new InMemoryDepartmentRepository();
        foreach (var a in new[] { _agent, _otherAgent })
        {
            departments.SetPolicy(a, new DepartmentPolicy(
                new Dictionary<Priority, TimeSpan> { [Priority.High] = TimeSpan.FromHours(2) }, 0.5, TimeSpan.FromDays(1)));
            departments.AddShift(a, new Shift(DateTimeOffset.Parse("2026-10-28T09:00:00Z"), DateTimeOffset.Parse("2026-10-28T17:00:00Z")));
        }
        _timeframes = new ResponseTimeframeCalculationService(departments);
    }

    private TicketService ServiceAt(DateTimeOffset now, IEventStore eventStore) =>
        new(eventStore, _timeframes, new FixedTimeProvider(now));

    private TicketId OpenAndAssign()
    {
        var service = ServiceAt(Opened, _store);
        var id = service.Open(_customer, [ProductId.Random()], "Cannot log in", Priority.High);
        service.Assign(id, _agent);
        return id;
    }

    /// <summary>Appends another agent's change between each fetch and the append that follows it.</summary>
    private sealed class ConcurrentEventStore(InMemoryEventStore inner, UserId agent, DateTimeOffset at) : IEventStore
    {
        public IReadOnlyList<TicketEvent> Fetch(TicketId id)
        {
            var events = inner.Fetch(id);
            inner.Append(id, [new TicketClosed(id, agent, at)], events.Count - 1);
            return events;
        }

        public void Append(TicketId id, IReadOnlyList<TicketEvent> newEvents, int expectedVersion) =>
            inner.Append(id, newEvents, expectedVersion);
    }

    [Fact]
    public void EveryChangeIsKeptInTheEventStore()
    {
        var id = OpenAndAssign();
        ServiceAt(Opened, _store).AddMessage(id, _customer, "Still broken");

        Assert.Equal(["TicketOpened", "TicketAssigned", "MessageAdded"], _store.Fetch(id).Select(e => e.GetType().Name));
    }

    [Fact]
    public void TheAgentsDeadlineIsInTheAssignedEvent()
    {
        var id = OpenAndAssign();

        var assigned = Assert.IsType<TicketAssigned>(_store.Fetch(id)[1]);
        Assert.Equal(Deadline, assigned.Timeframe.Deadline);
    }

    [Fact]
    public void AConcurrentAppendIsReportedAsAFailure()
    {
        var id = OpenAndAssign();

        var result = ServiceAt(Deadline, new ConcurrentEventStore(_store, _agent, Deadline)).Escalate(id, _customer);

        Assert.IsType<ExecutionResult.Failure>(result);
        Assert.IsType<TicketClosed>(_store.Fetch(id)[^1]);
    }

    [Fact]
    public void TheTicketAtAnEarlierVersionIsRebuiltFromItsFirstEvents()
    {
        var id = OpenAndAssign();
        ServiceAt(Opened, _store).Close(id, _customer);
        var service = ServiceAt(Opened, _store);

        Assert.Null(service.LoadAt(id, 0).AssignedAgent);
        Assert.Equal(TicketStatus.Open, service.LoadAt(id, 1).Status);
        Assert.Equal(TicketStatus.Closed, service.Load(id).Status);
    }

    [Fact]
    public void ANewProjectionReadsTheWholeHistory()
    {
        var id = OpenAndAssign();
        ServiceAt(Opened, _store).AddMessage(id, _customer, "Still broken");
        ServiceAt(Deadline, _store).EvaluateAutomaticActions(id, _otherAgent);
        ServiceAt(Deadline, _store).Close(id, _customer);

        var analysis = TicketAnalysisProjection.Of(_store.Fetch(id));

        Assert.Equal(1, analysis.Messages);
        Assert.Equal(1, analysis.Reassignments);
        Assert.True(analysis.WasEscalated);
        Assert.Equal(TicketStatus.Closed, analysis.Status);
        Assert.Equal(ServiceAt(Opened, _store).Load(id).Version, analysis.Version);
    }
}
