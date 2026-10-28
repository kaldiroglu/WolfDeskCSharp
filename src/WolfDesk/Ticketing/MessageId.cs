namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>A message's identity inside its ticket. A value object: two ids with the same value are equal.</summary>
public sealed record MessageId
{
    public Guid Value { get; }

    public MessageId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("An id cannot be empty", nameof(value));
        Value = value;
    }

    public static MessageId Random() => new(Guid.NewGuid());
}
