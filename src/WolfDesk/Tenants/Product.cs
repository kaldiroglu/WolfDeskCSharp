namespace dev.kaldiroglu.WolfDesk.Tenants;

/// <summary>A product the tenant supports. Saved as a row of the products table by its tenant.</summary>
public class Product
{
    private string _name = "";

    public Product(string name) : this(Guid.NewGuid(), name)
    {
    }

    internal Product(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A product needs a name");
            _name = value.Trim();
        }
    }
}
