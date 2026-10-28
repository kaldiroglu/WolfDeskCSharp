using dev.kaldiroglu.WolfDesk.Billing;
using dev.kaldiroglu.WolfDesk.Billing.InMemory;

namespace dev.kaldiroglu.WolfDesk.Tests.Billing;

public class ChargeTenantTests
{
    private static readonly ChargingPeriod October = new(2026, 10);

    private readonly TenantId _tenant = TenantId.Random();
    private readonly InMemoryBillingDatabase _db = new();

    public ChargeTenantTests() => _db.SetPricePerTicket(_tenant, 2.00m);

    private void OpenTickets(int count, DateTimeOffset at)
    {
        var record = new RecordTicketOpened(_db);
        for (var i = 0; i < count; i++) record.Execute(_tenant, Guid.NewGuid(), at);
    }

    [Fact]
    public void ChargesEveryTicketOpenedInThePeriodWithItsDiscount()
    {
        OpenTickets(501, DateTimeOffset.Parse("2026-10-15T10:00:00Z"));

        var invoice = new ChargeTenant(_db).Execute(_tenant, October);

        Assert.Equal(501, invoice.TicketsOpened);
        Assert.Equal(10, invoice.DiscountPercent);
        Assert.Equal(901.80m, invoice.Amount);   // 501 x 2.00 x 90%
        Assert.True(_db.IsPeriodClosed(_tenant, October));
    }

    [Fact]
    public void TicketsOfAnotherMonthAreNotCharged()
    {
        OpenTickets(3, DateTimeOffset.Parse("2026-10-31T23:59:59Z"));
        OpenTickets(2, DateTimeOffset.Parse("2026-11-01T00:00:00Z"));

        Assert.Equal(3, new ChargeTenant(_db).Execute(_tenant, October).TicketsOpened);
    }

    [Fact]
    public void ChargingTheSamePeriodTwiceReturnsTheFirstInvoice()
    {
        OpenTickets(10, DateTimeOffset.Parse("2026-10-02T09:00:00Z"));
        var charge = new ChargeTenant(_db);

        var first = charge.Execute(_tenant, October);
        OpenTickets(5, DateTimeOffset.Parse("2026-10-03T09:00:00Z"));
        var second = charge.Execute(_tenant, October);

        Assert.Equal(first, second);
    }

    private sealed class FailingDatabase : InMemoryBillingDatabase
    {
        public override void ClosePeriod(TenantId tenant, ChargingPeriod period) =>
            throw new InvalidOperationException("database timeout");
    }

    [Fact]
    public void AFailureAfterTheInvoiceRollsTheInvoiceBack()
    {
        var failing = new FailingDatabase();
        failing.SetPricePerTicket(_tenant, 1m);

        Assert.Throws<InvalidOperationException>(() => new ChargeTenant(failing).Execute(_tenant, October));
        Assert.Null(failing.FindInvoice(_tenant, October));
    }
}
