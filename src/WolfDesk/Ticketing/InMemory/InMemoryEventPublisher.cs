using dev.kaldiroglu.WolfDesk.Ticketing.PublishedLanguage;

namespace dev.kaldiroglu.WolfDesk.Ticketing.InMemory;

/// <summary>Mimics a message bus: it keeps the published messages and hands each to the subscribers.</summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly List<TicketingMessage> _published = new();
    private readonly List<Action<TicketingMessage>> _subscribers = new();

    public void Publish(TicketingMessage message)
    {
        _published.Add(message);
        foreach (var subscriber in _subscribers) subscriber(message);
    }

    public void Subscribe(Action<TicketingMessage> subscriber) => _subscribers.Add(subscriber);

    public IReadOnlyList<TicketingMessage> Published => _published.ToList();
}
