namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>A customer or a support agent. A value object: two ids with the same value are equal.</summary>
public sealed record UserId
{
    public Guid Value { get; }

    public UserId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("An id cannot be empty", nameof(value));
        Value = value;
    }

    public static UserId Random() => new(Guid.NewGuid());
}
