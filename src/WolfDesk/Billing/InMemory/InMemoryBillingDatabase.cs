namespace dev.kaldiroglu.WolfDesk.Billing.InMemory;

/// <summary>
/// Mimics a relational database with dictionaries. A transaction works on a copy of the
/// tables; commit replaces the tables with the copy, and rollback throws the copy away.
/// </summary>
public class InMemoryBillingDatabase : IBillingDatabase
{
    private sealed record OpenedTicket(TenantId Tenant, DateTimeOffset OpenedAt);

    private sealed record PeriodKey(TenantId Tenant, ChargingPeriod Period);

    private sealed class Tables
    {
        public Dictionary<TenantId, decimal> Prices { get; private init; } = new();
        public Dictionary<Guid, OpenedTicket> OpenedTickets { get; private init; } = new();
        public Dictionary<PeriodKey, Invoice> Invoices { get; private init; } = new();
        public HashSet<PeriodKey> ClosedPeriods { get; private init; } = new();

        public Tables Copy() => new()
        {
            Prices = new(Prices),
            OpenedTickets = new(OpenedTickets),
            Invoices = new(Invoices),
            ClosedPeriods = new(ClosedPeriods)
        };
    }

    private Tables _committed = new();
    private Tables? _working;

    /// <summary>Outside a transaction, reads and writes go straight to the committed tables.</summary>
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

    public void SetPricePerTicket(TenantId tenant, decimal price) => Current.Prices[tenant] = price;

    public decimal? FindPricePerTicket(TenantId tenant) =>
        Current.Prices.TryGetValue(tenant, out var price) ? price : null;

    public bool IsTicketRecorded(Guid ticketId) => Current.OpenedTickets.ContainsKey(ticketId);

    public void InsertOpenedTicket(TenantId tenant, Guid ticketId, DateTimeOffset openedAt)
    {
        if (!Current.OpenedTickets.TryAdd(ticketId, new OpenedTicket(tenant, openedAt)))
            throw new InvalidOperationException($"Duplicate key: ticket {ticketId}");
    }

    public int CountTicketsOpened(TenantId tenant, ChargingPeriod period) =>
        Current.OpenedTickets.Values.Count(t => t.Tenant == tenant && ChargingPeriod.Of(t.OpenedAt) == period);

    public Invoice? FindInvoice(TenantId tenant, ChargingPeriod period) =>
        Current.Invoices.GetValueOrDefault(new PeriodKey(tenant, period));

    public void InsertInvoice(Invoice invoice)
    {
        var key = new PeriodKey(invoice.Tenant, invoice.Period);
        if (!Current.Invoices.TryAdd(key, invoice))
            throw new InvalidOperationException($"Duplicate key: invoice {key}");
    }

    public virtual void ClosePeriod(TenantId tenant, ChargingPeriod period) =>
        Current.ClosedPeriods.Add(new PeriodKey(tenant, period));

    public bool IsPeriodClosed(TenantId tenant, ChargingPeriod period) =>
        Current.ClosedPeriods.Contains(new PeriodKey(tenant, period));
}
