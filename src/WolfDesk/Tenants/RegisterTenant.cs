namespace dev.kaldiroglu.WolfDesk.Tenants;

/// <summary>A transaction script that creates a tenant through its active record.</summary>
public sealed class RegisterTenant(TenantsDatabase db)
{
    public Guid Execute(string name, IEnumerable<string> ticketCategories)
    {
        try
        {
            db.StartTransaction();

            var tenant = new Tenant(db);
            tenant.Name = name;
            tenant.TicketCategories.AddRange(ticketCategories);
            tenant.Save();

            db.Commit();
            return tenant.Id;
        }
        catch
        {
            db.Rollback();
            throw;
        }
    }
}
