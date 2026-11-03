using dev.kaldiroglu.WolfDesk.Billing.BusinessLogic;
using dev.kaldiroglu.WolfDesk.Ticketing.PublishedLanguage;

namespace dev.kaldiroglu.WolfDesk.Billing.Presentation;

/// <summary>
/// Billing's anticorruption layer over ticketing's published language (chapter 9, p. 138):
/// it translates the one message billing needs into billing's own model, and ignores the rest.
/// It belongs to the presentation layer: a subscription to a message broker is one of the
/// ways a system receives requests (chapter 8, p. 119). It translates the message into a call
/// of a transaction script in the layer beneath, and nothing else: the script finds the
/// tenant of the ticket's first product. Messages arrive at least once, and
/// <see cref="RecordTicketOpened"/> counts a ticket once however often it is delivered.
/// </summary>
public sealed class TicketingAnticorruptionLayer(RecordTicketOpened recordTicketOpened)
{
    public void On(TicketingMessage message)
    {
        if (message is TicketOpenedMessage opened)
        {
            recordTicketOpened.Execute(opened.Products[0], opened.TicketId, opened.OccurredAt);
        }
    }
}
