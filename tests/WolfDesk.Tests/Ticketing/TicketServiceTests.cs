using dev.kaldiroglu.WolfDesk.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing;

public class TicketServiceTests
{
    private static readonly DateTimeOffset Opened = DateTimeOffset.Parse("2026-10-28T10:00:00Z");

    private readonly UserId _customer = UserId.Random();
    private readonly UserId _agent = UserId.Random();
    private readonly InMemoryTicketRepository _tickets = new();
    private readonly InMemoryEventPublisher _publisher = new();
    private readonly ResponseTimeframeCalculationService _timeframes;

    public TicketServiceTests()
    {
        var departments = new InMemoryDepartmentRepository();
        departments.SetPolicy(_agent, new DepartmentPolicy(
            new Dictionary<Priority, TimeSpan> { [Priority.High] = TimeSpan.FromHours(2) }, 0.5, TimeSpan.FromDays(1)));
        departments.AddShift(_agent, new Shift(DateTimeOffset.Parse("2026-10-28T09:00:00Z"), DateTimeOffset.Parse("2026-10-28T17:00:00Z")));
        _timeframes = new ResponseTimeframeCalculationService(departments);
    }

    private TicketService ServiceAt(DateTimeOffset now, ITicketRepository repository) =>
        new(repository, _timeframes, _publisher, new FixedTimeProvider(now));

    private TicketId OpenAndAssign()
    {
        var service = ServiceAt(Opened, _tickets);
        var id = service.Open(_customer, [ProductId.Random()], "Cannot log in", Priority.High);
        service.Assign(id, _agent);
        return id;
    }

    [Fact]
    public void TheEventsArePublishedAfterTheTicketIsSaved()
    {
        OpenAndAssign();

        Assert.Equal(["TicketOpened", "TicketAssigned"], _publisher.Published.Select(e => e.GetType().Name));
    }

    [Fact]
    public void TheAgentGetsADeadlineFromTheDomainService()
    {
        var id = OpenAndAssign();

        var assigned = Assert.IsType<TicketAssigned>(_publisher.Published[1]);
        Assert.Equal(id, assigned.TicketId);
        Assert.Equal(DateTimeOffset.Parse("2026-10-28T12:00:00Z"), assigned.ResponseDeadline);
    }

    [Fact]
    public void AnEscalationAfterTheDeadlineSucceeds()
    {
        var id = OpenAndAssign();

        var result = ServiceAt(DateTimeOffset.Parse("2026-10-28T12:00:00Z"), _tickets).Escalate(id, _customer);

        Assert.IsType<ExecutionResult.Success>(result);
        Assert.True(_tickets.Load(id).IsEscalated);
    }

    /// <summary>Another request saves the same ticket between this request's load and save.</summary>
    private sealed class RacingRepository(InMemoryTicketRepository tickets) : ITicketRepository
    {
        public Ticket Load(TicketId id)
        {
            var loaded = tickets.Load(id);
            tickets.Save(tickets.Load(id));
            return loaded;
        }

        public void Save(Ticket ticket) => tickets.Save(ticket);
    }

    [Fact]
    public void AConcurrentChangeIsReportedAsAFailure()
    {
        var id = OpenAndAssign();

        var result = ServiceAt(DateTimeOffset.Parse("2026-10-28T12:00:00Z"), new RacingRepository(_tickets))
            .Escalate(id, _customer);

        Assert.IsType<ExecutionResult.Failure>(result);
        Assert.False(_tickets.Load(id).IsEscalated);
    }

    [Fact]
    public void ARejectedEscalationIsReportedAsAFailure()
    {
        var id = OpenAndAssign();

        var result = ServiceAt(Opened, _tickets).Escalate(id, _customer);

        Assert.Equal(new ExecutionResult.Failure("The response deadline has not passed yet"), result);
    }
}
