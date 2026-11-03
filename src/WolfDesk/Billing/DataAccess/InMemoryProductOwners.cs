namespace dev.kaldiroglu.WolfDesk.Billing.DataAccess;

/// <summary>Mimics a table of the products each tenant supports, as billing keeps it.</summary>
public class InMemoryProductOwners : IProductOwners
{
    private readonly Dictionary<Guid, TenantId> _owners = new();

    public void Add(Guid productId, TenantId tenant) => _owners[productId] = tenant;

    public TenantId TenantOf(Guid productId) =>
        _owners.TryGetValue(productId, out var tenant)
            ? tenant
            : throw new ArgumentException($"No tenant supports product {productId}", nameof(productId));
}
