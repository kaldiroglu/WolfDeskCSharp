namespace dev.kaldiroglu.WolfDesk.Ticketing.Domain;

/// <summary>A command was rejected because it would break one of the ticket's rules.</summary>
public class TicketRuleViolationException(string message) : Exception(message);
