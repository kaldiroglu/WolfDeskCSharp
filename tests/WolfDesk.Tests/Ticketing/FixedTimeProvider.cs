namespace dev.kaldiroglu.WolfDesk.Tests.Ticketing;

/// <summary>A clock that always reads the same instant.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
