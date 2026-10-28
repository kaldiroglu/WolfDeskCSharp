namespace dev.kaldiroglu.WolfDesk.Ticketing.ReadModels;

/// <summary>
/// Projects the saved tickets into the agents' queues synchronously: each catch-up reads the
/// tickets saved after the last checkpoint, updates the queues, and stores the new
/// checkpoint. Resetting the checkpoint to 0 rebuilds the queues from scratch.
/// </summary>
public sealed class AgentQueueProjection(ITicketChangeFeed feed) : IAgentQueue
{
    private sealed record Row(UserId Agent, QueuedTicket Ticket);

    private readonly Dictionary<TicketId, Row> _rows = new();
    private readonly Lock _lock = new();
    private long _checkpoint;

    public long Checkpoint
    {
        get { lock (_lock) return _checkpoint; }
    }

    /// <summary>Applies the changes after the last checkpoint and returns how many there were.</summary>
    public int CatchUp()
    {
        lock (_lock)
        {
            var changes = feed.ChangesAfter(_checkpoint);
            foreach (var change in changes)
            {
                Project(change.Ticket);
                _checkpoint = change.Checkpoint;
            }
            return changes.Count;
        }
    }

    /// <summary>Wipes the queues; the next catch-up projects every ticket again.</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _rows.Clear();
            _checkpoint = 0;
        }
    }

    public IReadOnlyList<QueuedTicket> OpenTicketsOf(UserId agent)
    {
        lock (_lock)
        {
            return _rows.Values
                .Where(row => row.Agent == agent)
                .Select(row => row.Ticket)
                .OrderBy(ticket => ticket.Deadline)
                .ToList();
        }
    }

    private void Project(TicketSnapshot ticket)
    {
        if (ticket.Status != TicketStatus.Open || ticket.AssignedAgent is not { } agent)
        {
            _rows.Remove(ticket.Id);
            return;
        }
        _rows[ticket.Id] = new Row(agent, new QueuedTicket(ticket.Id, ticket.Title,
            ticket.Priority, ticket.ResponseTimeframe!.Deadline, ticket.Escalated));
    }
}
