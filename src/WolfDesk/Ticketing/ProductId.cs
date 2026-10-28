namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>A product of the tenant, referenced by its id only. A value object: two ids with the same value are equal.</summary>
public sealed record ProductId
{
    public Guid Value { get; }

    public ProductId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("An id cannot be empty", nameof(value));
        Value = value;
    }

    public static ProductId Random() => new(Guid.NewGuid());
}
