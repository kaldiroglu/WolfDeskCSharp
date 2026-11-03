using dev.kaldiroglu.WolfDesk.Ticketing.PublishedLanguage;

namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>
/// Billing's anticorruption layer over ticketing's published language (chapter 9, p. 138):
/// it translates the one message billing needs into billing's own model, and ignores the rest.
/// Ticketing knows a ticket's products; billing charges tenants. So the translation finds the
/// tenant from the ticket's first product. Messages arrive at least once, and
/// <see cref="RecordTicketOpened"/> counts a ticket once however often it is delivered.
/// </summary>
public sealed class TicketingAnticorruptionLayer(IProductOwners productOwners, RecordTicketOpened recordTicketOpened)
{
    public void On(TicketingMessage message)
    {
        if (message is TicketOpenedMessage opened)
        {
            var tenant = productOwners.TenantOf(opened.Products[0]);
            recordTicketOpened.Execute(tenant, opened.TicketId, opened.OccurredAt);
        }
    }
}
