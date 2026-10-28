namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>
/// Charges a tenant for the tickets opened in one charging period, with the volume
/// discount. Two writes, the invoice and the closed period, run in one transaction: either
/// both happen or neither does. Charging a period that already has an invoice returns that
/// invoice and changes nothing, so a repeated call cannot charge twice.
/// </summary>
public sealed class ChargeTenant(IBillingDatabase db)
{
    public Invoice Execute(TenantId tenant, ChargingPeriod period)
    {
        try
        {
            db.StartTransaction();

            var existing = db.FindInvoice(tenant, period);
            if (existing is not null)
            {
                db.Commit();
                return existing;
            }

            var tickets = db.CountTicketsOpened(tenant, period);
            var price = db.FindPricePerTicket(tenant)
                ?? throw new InvalidOperationException($"No price for {tenant}");
            var discount = VolumeDiscount.PercentFor(tickets);
            var amount = Math.Round(price * tickets * (100 - discount) / 100m, 2,
                MidpointRounding.AwayFromZero);

            var invoice = new Invoice(tenant, period, tickets, price, discount, amount);
            db.InsertInvoice(invoice);
            db.ClosePeriod(tenant, period);

            db.Commit();
            return invoice;
        }
        catch
        {
            db.Rollback();
            throw;
        }
    }
}
