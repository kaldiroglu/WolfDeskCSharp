namespace dev.kaldiroglu.WolfDesk.Ticketing.EventSourced;

/// <summary>
/// The ticket's state, projected from its events (the book's <c>TicketState</c>, p. 110).
/// Each event sets the fields it changes and increments the version; nothing else changes
/// them. The version is -1 before the first event, so <c>TicketOpened</c> is version 0.
/// </summary>
public sealed class TicketState
{
    /// <summary>A message as the ticket's events describe it.</summary>
    public sealed record Message(MessageId Id, UserId From, UserId To, string Body, DateTimeOffset SentAt,
        bool WasRead);

    private readonly List<Message> _messages = new();

    public TicketId? Id { get; private set; }
    public int Version { get; private set; } = -1;
    public UserId? Customer { get; private set; }
    public IReadOnlyList<ProductId> Products { get; private set; } = [];
    public string? Title { get; private set; }
    public Priority Priority { get; private set; }
    public UserId? AssignedAgent { get; private set; }
    public ResponseTimeframe? ResponseTimeframe { get; private set; }
    public bool IsEscalated { get; private set; }
    public TicketStatus Status { get; private set; }
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    /// <summary>An event of a kind not listed here is a bug, so it throws.</summary>
    internal void Apply(TicketEvent @event)
    {
        switch (@event)
        {
            case TicketOpened e:
                Id = e.TicketId;
                Customer = e.Customer;
                Products = e.Products;
                Title = e.Title;
                Priority = e.Priority;
                Status = TicketStatus.Open;
                break;
            case TicketAssigned e:
                AssignedAgent = e.Agent;
                ResponseTimeframe = e.Timeframe;
                break;
            case MessageAdded e:
                _messages.Add(new Message(e.MessageId, e.From, e.To, e.Body, e.OccurredAt, false));
                break;
            case MessageRead e:
                var i = _messages.FindIndex(m => m.Id == e.MessageId);
                _messages[i] = _messages[i] with { WasRead = true };
                break;
            case TicketEscalated:
                IsEscalated = true;
                break;
            case TicketReassigned e:
                AssignedAgent = e.NewAgent;
                break;
            case TicketClosed:
                Status = TicketStatus.Closed;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(@event), @event.GetType().Name, "Not a ticket event");
        }
        Version += 1;
    }
}
