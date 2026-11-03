namespace dev.kaldiroglu.WolfDesk.Billing;

/// <summary>Which tenant supports a product: what billing needs to know about tenants' products.</summary>
public interface IProductOwners
{
    TenantId TenantOf(Guid productId);
}
