namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>
/// Records that a tenant's ticket was opened, so that it can be charged.
/// The caller may repeat a call it believes has failed (chapter 5, "Implicit distributed
/// transactions"). The ticket's id makes the operation idempotent: recording the same
/// ticket twice counts it once.
/// </summary>
public sealed class RecordTicketOpened(IBillingDatabase db)
{
    public void Execute(TenantId tenant, Guid ticketId, DateTimeOffset openedAt)
    {
        try
        {
            db.StartTransaction();
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
