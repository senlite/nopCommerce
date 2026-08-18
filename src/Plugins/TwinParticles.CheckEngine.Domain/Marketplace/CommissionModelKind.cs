namespace TwinParticles.CheckEngine.Domain.Marketplace;

public enum CommissionModelKind
{
    Flat = 1,
    Percentage = 2,
    Tiered = 3,
    CategoryOverride = 4
}

public enum CommissionBasis
{
    PerOrder = 1,
    PerLineItem = 2,
    LineSubtotal = 3,
    OrderSubtotal = 4
}
