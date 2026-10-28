namespace dev.kaldiroglu.WolfDesk.Tenants;

/// <summary>
/// An active record: an object that wraps a tenant's rows, encapsulates the database
/// access, and adds a little logic on that data (Fowler, quoted in chapter 5, p. 69).
/// Its state is open: public getters and setters, and lists that callers change directly.
/// The logic stays small, validating the values assigned to its fields.
/// </summary>
public class Tenant
{
    private readonly TenantsDatabase _db;
    private string? _name;

    public Tenant(TenantsDatabase db) : this(db, Guid.NewGuid())
    {
    }

    private Tenant(TenantsDatabase db, Guid id)
    {
        _db = db;
        Id = id;
    }

    public static Tenant? Find(TenantsDatabase db, Guid id)
    {
        var row = db.SelectTenant(id);
        if (row is null) return null;
        var tenant = new Tenant(db, row.Id) { _name = row.Name };
        tenant.Products.AddRange(db.SelectProducts(id).Select(p => new Product(p.Id, p.Name)));
        tenant.TicketCategories.AddRange(db.SelectCategories(id).Select(c => c.Name));
        return tenant;
    }

    public Guid Id { get; }

    public string? Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A tenant needs a name");
            _name = value.Trim();
        }
    }

    public List<Product> Products { get; } = new();

    public List<string> TicketCategories { get; } = new();

    /// <summary>Writes the tenant row, its product rows and its category rows.</summary>
    public void Save()
    {
        if (_name is null) throw new InvalidOperationException("A tenant needs a name");
        _db.UpsertTenant(new TenantsDatabase.TenantRow(Id, _name));
        _db.ReplaceProducts(Id, Products.Select(p => new TenantsDatabase.ProductRow(p.Id, Id, p.Name)));
        _db.ReplaceCategories(Id, TicketCategories.Select(c => new TenantsDatabase.CategoryRow(Id, c)));
    }

    public void Delete() => _db.DeleteTenant(Id);
}
