namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>One of an agent's working periods, from the shift schedule (Preface, p. xix).</summary>
public sealed record Shift
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public Shift(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start) throw new ArgumentException("A shift ends after it starts");
        Start = start;
        End = end;
    }
}
