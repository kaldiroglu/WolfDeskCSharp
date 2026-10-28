namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>
/// The thin abstraction a transaction script may use to reach its storage (chapter 5,
/// p. 64). Changes made after <see cref="StartTransaction"/> become visible only on
/// <see cref="Commit"/>; <see cref="Rollback"/> discards them.
/// </summary>
public interface IBillingDatabase
{
    void StartTransaction();
    void Commit();
    void Rollback();

    void SetPricePerTicket(TenantId tenant, decimal price);
    decimal? FindPricePerTicket(TenantId tenant);

    bool IsTicketRecorded(Guid ticketId);
    void InsertOpenedTicket(TenantId tenant, Guid ticketId, DateTimeOffset openedAt);
    int CountTicketsOpened(TenantId tenant, ChargingPeriod period);

    Invoice? FindInvoice(TenantId tenant, ChargingPeriod period);
    void InsertInvoice(Invoice invoice);

    void ClosePeriod(TenantId tenant, ChargingPeriod period);
    bool IsPeriodClosed(TenantId tenant, ChargingPeriod period);
}
