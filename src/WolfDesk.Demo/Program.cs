namespace dev.kaldiroglu.WolfDesk.Demo;

/// <summary>Runs the demo: one story through WolfDesk's three bounded contexts.</summary>
public static class Program
{
    public static void Main() => new WolfDeskDemo(Console.Out).Run();
}
