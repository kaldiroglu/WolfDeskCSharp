namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// A message inside a ticket. An entity: it has an id, and its state changes when the
/// receiver reads it. Only its ticket can change it.
/// </summary>
public sealed class Message
{
    internal Message(MessageId id, UserId from, UserId to, string body, DateTimeOffset sentAt, bool wasRead)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("A message needs a body");
        Id = id;
        From = from;
        To = to;
        Body = body;
        SentAt = sentAt;
        WasRead = wasRead;
    }

    public MessageId Id { get; }
    public UserId From { get; }
    public UserId To { get; }
    public string Body { get; }
    public DateTimeOffset SentAt { get; }
    public bool WasRead { get; private set; }

    internal void MarkAsRead() => WasRead = true;
}
