namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>A calendar month in UTC.</summary>
public readonly record struct ChargingPeriod(int Year, int Month)
{
    public static ChargingPeriod Of(DateTimeOffset at)
    {
        var utc = at.ToUniversalTime();
        return new ChargingPeriod(utc.Year, utc.Month);
    }
}
