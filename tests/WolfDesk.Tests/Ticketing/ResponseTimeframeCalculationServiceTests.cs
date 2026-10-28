using dev.kaldiroglu.WolfDesk.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing;

public class ResponseTimeframeCalculationServiceTests
{
    private readonly UserId _agent = UserId.Random();
    private readonly ResponseTimeframeCalculationService _service;

    public ResponseTimeframeCalculationServiceTests()
    {
        var departments = new InMemoryDepartmentRepository();
        departments.SetPolicy(_agent, new DepartmentPolicy(
            new Dictionary<Priority, TimeSpan> { [Priority.High] = TimeSpan.FromHours(2), [Priority.Low] = TimeSpan.FromHours(8) },
            0.5, TimeSpan.FromDays(3)));
        departments.AddShift(_agent, new Shift(DateTimeOffset.Parse("2026-10-28T09:00:00Z"), DateTimeOffset.Parse("2026-10-28T17:00:00Z")));
        departments.AddShift(_agent, new Shift(DateTimeOffset.Parse("2026-10-29T09:00:00Z"), DateTimeOffset.Parse("2026-10-29T17:00:00Z")));
        _service = new ResponseTimeframeCalculationService(departments);
    }

    [Fact]
    public void TheDeadlineFallsInsideTheCurrentShift()
    {
        var timeframe = _service.CalculateAgentResponseDeadline(_agent, Priority.High, false,
            DateTimeOffset.Parse("2026-10-28T10:00:00Z"));

        Assert.Equal(DateTimeOffset.Parse("2026-10-28T12:00:00Z"), timeframe.Deadline);
    }

    [Fact]
    public void TimeOutsideTheShiftsDoesNotCount()
    {
        var timeframe = _service.CalculateAgentResponseDeadline(_agent, Priority.High, false,
            DateTimeOffset.Parse("2026-10-28T16:00:00Z"));   // one working hour left today

        Assert.Equal(DateTimeOffset.Parse("2026-10-29T10:00:00Z"), timeframe.Deadline);
    }

    [Fact]
    public void AnEscalatedTicketGetsTheEscalationFactor()
    {
        var timeframe = _service.CalculateAgentResponseDeadline(_agent, Priority.High, true,
            DateTimeOffset.Parse("2026-10-28T10:00:00Z"));

        Assert.Equal(DateTimeOffset.Parse("2026-10-28T11:00:00Z"), timeframe.Deadline);
    }

    [Fact]
    public void NotEnoughShiftsIsAnError() =>
        Assert.Throws<InvalidOperationException>(() => _service.CalculateAgentResponseDeadline(_agent,
            Priority.Low, false, DateTimeOffset.Parse("2026-10-29T16:00:00Z")));
}
