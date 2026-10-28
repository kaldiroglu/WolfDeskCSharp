namespace dev.kaldiroglu.WolfDesk.Tenants;

/// <summary>A transaction script that adds a product to the list a tenant supports.</summary>
public sealed class AddSupportedProduct(TenantsDatabase db)
{
    public Guid Execute(Guid tenantId, string productName)
    {
        try
        {
            db.StartTransaction();

            var tenant = Tenant.Find(db, tenantId)
                ?? throw new ArgumentException($"No tenant {tenantId}");
            var product = new Product(productName);
            tenant.Products.Add(product);
            tenant.Save();

            db.Commit();
            return product.Id;
        }
        catch
        {
            db.Rollback();
            throw;
        }
    }
}
