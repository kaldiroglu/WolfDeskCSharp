using System.Globalization;
using dev.kaldiroglu.WolfDesk.Billing.BusinessLogic;
using dev.kaldiroglu.WolfDesk.Billing.DataAccess;
using dev.kaldiroglu.WolfDesk.Billing.Presentation;
using dev.kaldiroglu.WolfDesk.Tenants.DataAccess;
using dev.kaldiroglu.WolfDesk.Tenants.Service;
using dev.kaldiroglu.WolfDesk.Ticketing.Application;
using dev.kaldiroglu.WolfDesk.Ticketing.Domain.Events;
using dev.kaldiroglu.WolfDesk.Ticketing.Domain;
using dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

namespace dev.kaldiroglu.WolfDesk.Demo;

/// <summary>
/// One story through WolfDesk's three bounded contexts, printed step by step.
/// This class is the composition root: the only place that wires the contexts to their
/// in-memory adapters, as a deployment would wire them to real ones. A tenant registers a
/// product, a customer opens a ticket, the agent misses the response deadline, the customer
/// escalates, and billing charges the tenant once the outbox relay has run.
/// </summary>
public sealed class WolfDeskDemo(TextWriter output)
{
    internal static readonly DateTimeOffset Opened = DateTimeOffset.Parse("2026-10-28T10:00:00Z", CultureInfo.InvariantCulture);
    internal static readonly ChargingPeriod Period = new(2026, 10);
    internal const decimal PricePerTicket = 2.00m;

    /// <summary>Runs the story and returns the tenant's invoice for the period.</summary>
    public Invoice Run()
    {
        // Tenants: a tenant and the product its customers open tickets about.
        var tenantsDb = new TenantsDatabase();
        var tenantId = new RegisterTenant(tenantsDb).Execute("Acme", ["Login", "Billing"]);
        var productId = new AddSupportedProduct(tenantsDb).Execute(tenantId, "Acme Mail");
        Step("Tenants", "Acme registered with the product Acme Mail");

        // Billing: the tenant's price, and the product's owner for the anticorruption layer.
        var billingDb = new InMemoryBillingDatabase();
        var tenant = new TenantId(tenantId);
        billingDb.SetPricePerTicket(tenant, PricePerTicket);
        var owners = new InMemoryProductOwners();
        owners.Add(productId, tenant);

        // Ticketing: the agent's department policy and shift, the repository with its outbox.
        var agent = UserId.Random();
        var customer = UserId.Random();
        var departments = new InMemoryDepartmentRepository();
        departments.SetPolicy(agent, new DepartmentPolicy(
            new Dictionary<Priority, TimeSpan> { [Priority.High] = TimeSpan.FromHours(2) }, 0.5, TimeSpan.FromDays(1)));
        departments.AddShift(agent, new Shift(At("09:00"), At("17:00")));
        var tickets = new InMemoryTicketRepository();
        var timeframes = new ResponseTimeframeCalculationService(departments);

        // The message bus: billing subscribes to ticketing's published language.
        var bus = new InMemoryEventPublisher();
        bus.Subscribe(new TicketingAnticorruptionLayer(new RecordTicketOpened(billingDb, owners)).On);

        var at10 = Service(tickets, timeframes, Opened);
        var ticket = at10.Open(customer, [new ProductId(productId)], "Cannot log in", Priority.High);
        Step("Ticketing", "10:00 The customer opens a high-priority ticket: Cannot log in");
        at10.Assign(ticket, agent);
        var deadline = tickets.Unpublished().Select(record => record.Event)
            .OfType<TicketAssigned>().First().ResponseDeadline;
        Step("Ticketing", "10:00 An agent is assigned; the response deadline is "
            + deadline.ToUniversalTime().ToString("HH:mm", CultureInfo.InvariantCulture));
        at10.AddMessage(ticket, customer, "I cannot log in since this morning.");
        Step("Ticketing", "10:00 The customer writes to the agent");

        var early = Service(tickets, timeframes, At("11:00")).Escalate(ticket, customer);
        Step("Ticketing", "11:00 The customer asks for an escalation: " + Describe(early));

        var late = Service(tickets, timeframes, At("12:00")).Escalate(ticket, customer);
        Step("Ticketing", "12:00 The customer asks again: " + Describe(late));

        Service(tickets, timeframes, At("12:30")).Close(ticket, agent);
        Step("Ticketing", "12:30 The agent closes the ticket");

        Step("Billing", "Tickets counted before the relay runs: " + billingDb.CountTicketsOpened(tenant, Period));
        new OutboxRelay(tickets, bus).PublishPending();
        var published = bus.Published.Select(message => message.GetType().Name);
        Step("Ticketing", "The outbox relay publishes " + string.Join(", ", published));
        Step("Billing", "Tickets counted after the relay runs: " + billingDb.CountTicketsOpened(tenant, Period));

        var invoice = new ChargeTenant(billingDb).Execute(tenant, Period);
        Step("Billing", $"Invoice for {invoice.Period.Year}-{invoice.Period.Month:00}: "
            + $"{invoice.TicketsOpened} ticket(s) at {Money(invoice.PricePerTicket)}, "
            + $"discount {invoice.DiscountPercent}%, amount {Money(invoice.Amount)}");
        return invoice;
    }

    private static DateTimeOffset At(string time) =>
        DateTimeOffset.Parse($"2026-10-28T{time}:00Z", CultureInfo.InvariantCulture);

    private static TicketService Service(InMemoryTicketRepository tickets,
        ResponseTimeframeCalculationService timeframes, DateTimeOffset now) =>
        new(tickets, timeframes, new FixedTimeProvider(now));

    private static string Describe(ExecutionResult result) => result switch
    {
        ExecutionResult.Success => "escalated",
        ExecutionResult.Failure failure => "refused, " + failure.Reason.ToLowerInvariant(),
        _ => throw new ArgumentOutOfRangeException(nameof(result)),
    };

    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private void Step(string context, string text) => output.WriteLine($"{"[" + context + "]",-12} {text}");

    /// <summary>A clock that always reads the same instant: each step of the story runs at its own time.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
