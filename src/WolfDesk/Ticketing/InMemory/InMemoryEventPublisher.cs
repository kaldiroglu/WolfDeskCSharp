namespace dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

/// <summary>Mimics a message bus by keeping the published events in a list.</summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly List<DomainEvent> _published = new();

    public void Publish(DomainEvent domainEvent) => _published.Add(domainEvent);

    public IReadOnlyList<DomainEvent> Published => _published.ToList();
}
