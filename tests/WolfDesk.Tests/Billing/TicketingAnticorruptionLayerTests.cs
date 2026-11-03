using dev.kaldiroglu.WolfDesk.Billing;
using dev.kaldiroglu.WolfDesk.Billing.InMemory;
using dev.kaldiroglu.WolfDesk.Ticketing.PublishedLanguage;

namespace dev.kaldiroglu.WolfDesk.Tests.Billing;

public class TicketingAnticorruptionLayerTests
{
    private static readonly DateTimeOffset Opened = DateTimeOffset.Parse("2026-10-20T12:00:00Z");

    private readonly InMemoryBillingDatabase _db = new();
    private readonly InMemoryProductOwners _owners = new();
    private readonly TenantId _tenant = TenantId.Random();
    private readonly Guid _product = Guid.NewGuid();
    private readonly TicketingAnticorruptionLayer _acl;

    public TicketingAnticorruptionLayerTests()
    {
        _owners.Add(_product, _tenant);
        _acl = new TicketingAnticorruptionLayer(_owners, new RecordTicketOpened(_db));
    }

    private TicketOpenedMessage OpenedMessage(Guid ticket) => new(ticket, [_product], "High", Opened);

    [Fact]
    public void AnOpenedTicketIsRecordedForTheTenantOfItsProduct()
    {
        _acl.On(OpenedMessage(Guid.NewGuid()));

        Assert.Equal(1, _db.CountTicketsOpened(_tenant, new ChargingPeriod(2026, 10)));
    }

    [Fact]
    public void AMessageDeliveredTwiceIsCountedOnce()
    {
        var ticket = Guid.NewGuid();

        _acl.On(OpenedMessage(ticket));
        _acl.On(OpenedMessage(ticket));

        Assert.Equal(1, _db.CountTicketsOpened(_tenant, new ChargingPeriod(2026, 10)));
    }

    [Fact]
    public void AMessageBillingDoesNotNeedIsIgnored()
    {
        _acl.On(new TicketClosedMessage(Guid.NewGuid(), Opened));

        Assert.Equal(0, _db.CountTicketsOpened(_tenant, new ChargingPeriod(2026, 10)));
    }
}
