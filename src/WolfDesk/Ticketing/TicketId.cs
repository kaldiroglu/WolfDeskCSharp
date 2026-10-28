namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>A ticket's identity. A value object: two ids with the same value are equal.</summary>
public sealed record TicketId
{
    public Guid Value { get; }

    public TicketId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("An id cannot be empty", nameof(value));
        Value = value;
    }

    public static TicketId Random() => new(Guid.NewGuid());
}
