using dev.kaldiroglu.WolfDesk.Billing;

namespace dev.kaldiroglu.WolfDesk.Tests.Billing;

public class VolumeDiscountTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(500, 0)]
    [InlineData(501, 10)]
    [InlineData(750, 10)]
    [InlineData(751, 20)]
    [InlineData(1000, 20)]
    [InlineData(1001, 30)]
    [InlineData(5000, 30)]
    public void DiscountsStartAboveEachThreshold(int ticketsOpened, int expectedPercent) =>
        Assert.Equal(expectedPercent, VolumeDiscount.PercentFor(ticketsOpened));
}
