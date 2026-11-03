using dev.kaldiroglu.WolfDesk.Billing;
using dev.kaldiroglu.WolfDesk.Billing.InMemory;
using dev.kaldiroglu.WolfDesk.Tests.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

namespace dev.kaldiroglu.WolfDesk.Tests;

/// <summary>Two bounded contexts, wired the way a deployment would: ticketing's outbox to billing.</summary>
public class TicketingToBillingTests
{
    [Fact]
    public void ATicketOpenedInTicketingIsCountedByBillingOnceTheRelayRuns()
    {
        var tickets = new InMemoryTicketRepository();
        var bus = new InMemoryEventPublisher();
        var billing = new InMemoryBillingDatabase();
        var owners = new InMemoryProductOwners();
        var tenant = TenantId.Random();
        var product = ProductId.Random();
        owners.Add(product.Value, tenant);
        bus.Subscribe(new TicketingAnticorruptionLayer(owners, new RecordTicketOpened(billing)).On);
        var service = new TicketService(tickets,
            new ResponseTimeframeCalculationService(new InMemoryDepartmentRepository()),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-10-20T12:00:00Z")));

        service.Open(UserId.Random(), [product], "Cannot log in", Priority.High);
        Assert.Equal(0, billing.CountTicketsOpened(tenant, new ChargingPeriod(2026, 10)));

        new OutboxRelay(tickets, bus).PublishPending();
        Assert.Equal(1, billing.CountTicketsOpened(tenant, new ChargingPeriod(2026, 10)));
    }
}
