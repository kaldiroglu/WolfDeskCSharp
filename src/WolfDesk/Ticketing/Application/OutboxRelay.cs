namespace dev.kaldiroglu.WolfDesk.Ticketing.Application;

/// <summary>
/// The publishing relay of the outbox pattern, pull-based (chapter 9, pp. 145–146): it reads
/// the unpublished events, publishes their public versions, and marks them as published.
/// If it fails after publishing an event but before marking it, the next run publishes the
/// event again: delivery is at least once (p. 147), so consumers must be idempotent.
/// </summary>
public sealed class OutboxRelay(IOutbox outbox, IEventPublisher publisher)
{
    public void PublishPending()
    {
        foreach (var record in outbox.Unpublished())
        {
            var message = PublishedLanguageTranslator.Translate(record.Event);
            if (message is not null) publisher.Publish(message);
            outbox.MarkPublished(record.Position);
        }
    }
}
