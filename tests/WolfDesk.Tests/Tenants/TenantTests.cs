using dev.kaldiroglu.WolfDesk.Tenants;

namespace dev.kaldiroglu.WolfDesk.Tests.Tenants;

public class TenantTests
{
    private readonly TenantsDatabase _db = new();

    [Fact]
    public void ARegisteredTenantIsFoundWithItsCategories()
    {
        var id = new RegisterTenant(_db).Execute("Acme", ["Billing", "Technical"]);

        var tenant = Tenant.Find(_db, id)!;
        Assert.Equal("Acme", tenant.Name);
        Assert.Equal(["Billing", "Technical"], tenant.TicketCategories);
    }

    [Fact]
    public void ASupportedProductIsSavedWithItsTenant()
    {
        var id = new RegisterTenant(_db).Execute("Acme", []);

        new AddSupportedProduct(_db).Execute(id, "Acme Mobile");

        Assert.Equal(["Acme Mobile"], Tenant.Find(_db, id)!.Products.Select(p => p.Name));
    }

    [Fact]
    public void ABlankNameIsRejected() =>
        Assert.Throws<ArgumentException>(() => new RegisterTenant(_db).Execute(" ", []));

    [Fact]
    public void AnUnknownTenantRollsTheTransactionBack()
    {
        var script = new AddSupportedProduct(_db);

        Assert.Throws<ArgumentException>(() => script.Execute(Guid.NewGuid(), "X"));
        // The transaction was rolled back, so a new one can start.
        new RegisterTenant(_db).Execute("Acme", []);
    }

    [Fact]
    public void ChangesThatAreRolledBackAreNotKept()
    {
        var id = new RegisterTenant(_db).Execute("Acme", ["Billing"]);
        _db.StartTransaction();
        var tenant = Tenant.Find(_db, id)!;
        tenant.TicketCategories.Add("Sales");
        tenant.Save();
        _db.Rollback();

        Assert.Equal(["Billing"], Tenant.Find(_db, id)!.TicketCategories);
    }

    [Fact]
    public void ADeletedTenantIsGone()
    {
        var id = new RegisterTenant(_db).Execute("Acme", ["Billing"]);

        Tenant.Find(_db, id)!.Delete();

        Assert.Null(Tenant.Find(_db, id));
    }
}
