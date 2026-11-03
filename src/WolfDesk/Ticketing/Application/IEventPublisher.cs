using dev.kaldiroglu.WolfDesk.Ticketing.PublishedLanguage;

namespace dev.kaldiroglu.WolfDesk.Ticketing.Application;

/// <summary>Publishes ticketing's public messages to the rest of the system, through a message bus.</summary>
public interface IEventPublisher
{
    void Publish(TicketingMessage message);
}
