namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>Command: give the ticket to an agent, with the time the agent has to respond.</summary>
public sealed record AssignAgent(UserId Agent, ResponseTimeframe Timeframe, DateTimeOffset At);

/// <summary>Command: the customer or the assigned agent writes to the other.</summary>
public sealed record AddMessage(UserId From, string Body, DateTimeOffset At);

/// <summary>Command: the receiver of a message has read it.</summary>
public sealed record AcknowledgeMessage(MessageId MessageId, UserId By, DateTimeOffset At);

/// <summary>Command: the customer asks to escalate the ticket.</summary>
public sealed record RequestEscalation(UserId RequestedBy, DateTimeOffset At);

/// <summary>
/// Command: apply the rules that depend on time. The application service picks the agent
/// that would take over; the ticket decides whether a reassignment is due.
/// </summary>
public sealed record EvaluateAutomaticActions(DateTimeOffset At, UserId ReplacementAgent);

/// <summary>Command: the customer or the agent closes the ticket.</summary>
public sealed record CloseTicket(UserId ClosedBy, DateTimeOffset At);
