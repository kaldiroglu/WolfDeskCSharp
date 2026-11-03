using dev.kaldiroglu.WolfDesk.Billing.DataAccess;

namespace dev.kaldiroglu.WolfDesk.Billing.BusinessLogic;

/// <summary>
/// Records that a tenant's ticket was opened, so that it can be charged. Ticketing knows a
/// ticket's products; billing charges tenants. So the script finds the tenant that supports
/// the ticket's product, and records the ticket for that tenant.
/// The caller may repeat a call it believes has failed (chapter 5, "Implicit distributed
/// transactions"). The ticket's id makes the operation idempotent: recording the same
/// ticket twice counts it once.
/// </summary>
public sealed class RecordTicketOpened(IBillingDatabase db, IProductOwners productOwners)
{
    public void Execute(Guid productId, Guid ticketId, DateTimeOffset openedAt)
    {
        try
        {
            db.StartTransaction();
            var tenant = productOwners.TenantOf(productId);
            if (!db.IsTicketRecorded(ticketId))
            {
                db.InsertOpenedTicket(tenant, ticketId, openedAt);
            }
            db.Commit();
        }
        catch
        {
            db.Rollback();
            throw;
        }
    }
}
