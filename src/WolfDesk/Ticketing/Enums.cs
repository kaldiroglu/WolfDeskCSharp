namespace dev.kaldiroglu.WolfDesk.Ticketing;

public enum Priority
{
    Low,
    Medium,
    High
}

public enum TicketStatus
{
    Open,
    Closed
}

/// <summary>Why a ticket was escalated: the response deadline passed, or the customer asked.</summary>
public enum EscalationReason
{
    MissedSla,
    CustomerRequest
}
