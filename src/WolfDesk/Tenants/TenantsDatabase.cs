namespace dev.kaldiroglu.WolfDesk.Tenants;

/// <summary>
/// The data access layer the active records are coupled to. It mimics three tables,
/// tenants, products and ticket categories, with collections. A transaction works on a copy
/// of the tables; commit keeps the copy, rollback throws it away.
/// </summary>
public class TenantsDatabase
{
    internal sealed record TenantRow(Guid Id, string Name);

    internal sealed record ProductRow(Guid Id, Guid TenantId, string Name);

    internal sealed record CategoryRow(Guid TenantId, string Name);

    private sealed class Tables
    {
        public Dictionary<Guid, TenantRow> Tenants { get; private init; } = new();
        public Dictionary<Guid, ProductRow> Products { get; private init; } = new();
        public List<CategoryRow> Categories { get; private init; } = new();

        public Tables Copy() => new()
        {
            Tenants = new(Tenants),
            Products = new(Products),
            Categories = new(Categories)
        };
    }

    private Tables _committed = new();
    private Tables? _working;

    private Tables Current => _working ?? _committed;

    public void StartTransaction()
    {
        if (_working is not null) throw new InvalidOperationException("A transaction is already open");
        _working = _committed.Copy();
    }

    public void Commit()
    {
        _committed = Current;
        _working = null;
    }

    public void Rollback() => _working = null;

    internal TenantRow? SelectTenant(Guid id) => Current.Tenants.GetValueOrDefault(id);

    internal List<ProductRow> SelectProducts(Guid tenantId) =>
        Current.Products.Values.Where(p => p.TenantId == tenantId).ToList();

    internal List<CategoryRow> SelectCategories(Guid tenantId) =>
        Current.Categories.Where(c => c.TenantId == tenantId).ToList();

    internal void UpsertTenant(TenantRow row) => Current.Tenants[row.Id] = row;

    internal void ReplaceProducts(Guid tenantId, IEnumerable<ProductRow> rows)
    {
        foreach (var id in Current.Products.Values.Where(p => p.TenantId == tenantId).Select(p => p.Id).ToList())
            Current.Products.Remove(id);
        foreach (var row in rows) Current.Products[row.Id] = row;
    }

    internal void ReplaceCategories(Guid tenantId, IEnumerable<CategoryRow> rows)
    {
        Current.Categories.RemoveAll(c => c.TenantId == tenantId);
        Current.Categories.AddRange(rows);
    }

    internal void DeleteTenant(Guid tenantId)
    {
        ReplaceProducts(tenantId, []);
        ReplaceCategories(tenantId, []);
        Current.Tenants.Remove(tenantId);
    }
}
