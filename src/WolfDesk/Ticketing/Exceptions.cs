namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>A command was rejected because it would break one of the ticket's rules.</summary>
public class TicketRuleViolationException(string message) : Exception(message);

/// <summary>Someone else saved the ticket after it was loaded.</summary>
public class ConcurrencyException(TicketId id, int expectedVersion, int actualVersion)
    : Exception($"Ticket {id.Value} is at version {actualVersion}, not {expectedVersion}");

public class TicketNotFoundException(TicketId id) : Exception($"No ticket {id.Value}");
