using dev.kaldiroglu.WolfDesk.Ticketing;

namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing;

public class ValueObjectTests
{
    [Fact]
    public void TwoIdsWithTheSameValueAreEqual()
    {
        var value = Guid.NewGuid();
        Assert.Equal(new TicketId(value), new TicketId(value));
        Assert.NotEqual(new TicketId(value), TicketId.Random());
    }

    [Fact]
    public void AnIdCannotBeEmpty() =>
        Assert.Throws<ArgumentException>(() => new UserId(Guid.Empty));

    [Fact]
    public void ATimeframeEndsAfterItStarts()
    {
        var t = DateTimeOffset.Parse("2026-10-28T20:00:00Z");
        Assert.Throws<ArgumentException>(() => new ResponseTimeframe(t, t));
    }

    [Fact]
    public void TheRemainingTimeIsAShareOfTheWholeTimeframe()
    {
        var timeframe = new ResponseTimeframe(DateTimeOffset.Parse("2026-10-28T20:00:00Z"),
            DateTimeOffset.Parse("2026-10-28T22:00:00Z"));

        Assert.Equal(1.0, timeframe.RemainingTimePercentage(DateTimeOffset.Parse("2026-10-28T20:00:00Z")));
        Assert.Equal(0.5, timeframe.RemainingTimePercentage(DateTimeOffset.Parse("2026-10-28T21:00:00Z")));
        Assert.Equal(-0.5, timeframe.RemainingTimePercentage(DateTimeOffset.Parse("2026-10-28T23:00:00Z")));
    }
}
