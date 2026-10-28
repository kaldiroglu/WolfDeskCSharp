namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>A tenant, as billing knows it.</summary>
public sealed record TenantId
{
    public Guid Value { get; }

    public TenantId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("A tenant id cannot be empty", nameof(value));
        Value = value;
    }

    public static TenantId Random() => new(Guid.NewGuid());
}
