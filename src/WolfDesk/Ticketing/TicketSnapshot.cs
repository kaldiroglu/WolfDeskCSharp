namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>
/// A ticket's state as plain data, for the repository to store and load. The domain model
/// itself never depends on how it is stored.
/// </summary>
public sealed record TicketSnapshot(TicketId Id, int Version, UserId Customer,
    IReadOnlyList<ProductId> Products, string Title, Priority Priority, UserId? AssignedAgent,
    ResponseTimeframe? ResponseTimeframe, bool Escalated, TicketStatus Status,
    IReadOnlyList<TicketSnapshot.MessageSnapshot> Messages)
{
    public sealed record MessageSnapshot(MessageId Id, UserId From, UserId To, string Body,
        DateTimeOffset SentAt, bool WasRead);

    /// <summary>Equal when every field and every message is equal.</summary>
    public bool Equals(TicketSnapshot? other) =>
        other is not null && Id == other.Id && Version == other.Version && Customer == other.Customer
        && Products.SequenceEqual(other.Products) && Title == other.Title && Priority == other.Priority
        && AssignedAgent == other.AssignedAgent && ResponseTimeframe == other.ResponseTimeframe
        && Escalated == other.Escalated && Status == other.Status && Messages.SequenceEqual(other.Messages);

    public override int GetHashCode() => HashCode.Combine(Id, Version);
}
