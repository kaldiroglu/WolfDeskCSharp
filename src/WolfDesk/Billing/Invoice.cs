namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>What a tenant owes for one charging period.</summary>
public sealed record Invoice(TenantId Tenant, ChargingPeriod Period, int TicketsOpened,
    decimal PricePerTicket, int DiscountPercent, decimal Amount);
