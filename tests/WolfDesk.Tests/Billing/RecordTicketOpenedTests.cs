using dev.kaldiroglu.WolfDesk.Billing;
using dev.kaldiroglu.WolfDesk.Billing.InMemory;

namespace dev.kaldiroglu.WolfDesk.Tests.Billing;

public class RecordTicketOpenedTests
{
    [Fact]
    public void RecordingTheSameTicketTwiceCountsItOnce()
    {
        var db = new InMemoryBillingDatabase();
        var tenant = TenantId.Random();
        var ticket = Guid.NewGuid();
        var at = DateTimeOffset.Parse("2026-10-20T12:00:00Z");
        var record = new RecordTicketOpened(db);

        record.Execute(tenant, ticket, at);
        record.Execute(tenant, ticket, at);   // the caller retried

        Assert.Equal(1, db.CountTicketsOpened(tenant, new ChargingPeriod(2026, 10)));
    }
}
