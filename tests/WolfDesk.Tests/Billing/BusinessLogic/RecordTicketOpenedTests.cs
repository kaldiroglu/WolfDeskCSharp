using dev.kaldiroglu.WolfDesk.Billing.BusinessLogic;
using dev.kaldiroglu.WolfDesk.Billing.DataAccess;

namespace dev.kaldiroglu.WolfDesk.Tests.Billing.BusinessLogic;

public class RecordTicketOpenedTests
{
    [Fact]
    public void RecordingTheSameTicketTwiceCountsItOnce()
    {
        var db = new InMemoryBillingDatabase();
        var tenant = TenantId.Random();
        var product = Guid.NewGuid();
        var owners = new InMemoryProductOwners();
        owners.Add(product, tenant);
        var ticket = Guid.NewGuid();
        var at = DateTimeOffset.Parse("2026-10-20T12:00:00Z");
        var record = new RecordTicketOpened(db, owners);

        record.Execute(product, ticket, at);
        record.Execute(product, ticket, at);   // the caller retried

        Assert.Equal(1, db.CountTicketsOpened(tenant, new ChargingPeriod(2026, 10)));
    }
}
