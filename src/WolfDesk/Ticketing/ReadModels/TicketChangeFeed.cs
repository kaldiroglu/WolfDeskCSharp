namespace dev.kaldiroglu.WolfDesk.Ticketing.ReadModels;

/// <summary>A saved ticket with the checkpoint its row was written at.</summary>
public sealed record TicketChange(long Checkpoint, TicketSnapshot Ticket);

/// <summary>
/// The port a projection reads the command execution model through: the tickets saved
/// after a checkpoint, as in the catch-up subscription of Figures 8-13 and 8-14.
/// </summary>
public interface ITicketChangeFeed
{
    /// <summary>The tickets written after <paramref name="checkpoint"/>, lowest checkpoint first.</summary>
    IReadOnlyList<TicketChange> ChangesAfter(long checkpoint);
}
