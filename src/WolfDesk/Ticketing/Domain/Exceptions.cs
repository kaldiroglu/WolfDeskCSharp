namespace dev.kaldiroglu.WolfDesk.Ticketing.Domain;

/// <summary>Someone else saved the ticket after it was loaded.</summary>
public class ConcurrencyException(TicketId id, int expectedVersion, int actualVersion)
    : Exception($"Ticket {id.Value} is at version {actualVersion}, not {expectedVersion}");

public class TicketNotFoundException(TicketId id) : Exception($"No ticket {id.Value}");
