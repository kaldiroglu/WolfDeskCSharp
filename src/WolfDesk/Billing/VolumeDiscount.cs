namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>
/// The automatic volume discounts for monthly tickets: 10% for opening more than 500
/// tickets, 20% for more than 750, and 30% for more than 1,000 (Preface, p. xviii).
/// </summary>
public static class VolumeDiscount
{
    public static int PercentFor(int ticketsOpened) => ticketsOpened switch
    {
        > 1_000 => 30,
        > 750 => 20,
        > 500 => 10,
        _ => 0
    };
}
