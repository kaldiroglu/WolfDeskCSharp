namespace dev.kaldiroglu.WolfDesk.Ticketing.ReadModels;

/// <summary>One open ticket as an agent's queue shows it.</summary>
public sealed record QueuedTicket(TicketId Id, string Title, Priority Priority,
    DateTimeOffset Deadline, bool Escalated);

/// <summary>A read model: each agent's open tickets. It has queries only; no command changes it.</summary>
public interface IAgentQueue
{
    /// <summary>The agent's open tickets, earliest deadline first.</summary>
    IReadOnlyList<QueuedTicket> OpenTicketsOf(UserId agent);
}
