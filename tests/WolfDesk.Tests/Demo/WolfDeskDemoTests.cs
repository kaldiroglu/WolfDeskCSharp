using dev.kaldiroglu.WolfDesk.Demo;

namespace dev.kaldiroglu.WolfDesk.Tests.Demo;

public class WolfDeskDemoTests
{
    [Fact]
    public void TheDemoRunsTheStoryAndChargesTheTenantForOneTicket()
    {
        var writer = new StringWriter();
        var invoice = new WolfDeskDemo(writer).Run();
        var output = writer.ToString();

        Assert.Equal(1, invoice.TicketsOpened);
        Assert.Equal(0, invoice.DiscountPercent);
        Assert.Equal(2.00m, invoice.Amount);
        Assert.Contains("An agent is assigned; the response deadline is 12:00", output);
        Assert.Contains("11:00 The customer asks for an escalation: refused, the response deadline has not passed yet", output);
        Assert.Contains("12:00 The customer asks again: escalated", output);
        Assert.Contains("Tickets counted before the relay runs: 0", output);
        Assert.Contains("The outbox relay publishes TicketOpenedMessage, TicketEscalatedMessage, TicketClosedMessage", output);
        Assert.Contains("Tickets counted after the relay runs: 1", output);
    }
}
