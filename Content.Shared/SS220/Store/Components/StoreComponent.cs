namespace Content.Shared.Store.Components;

public sealed partial class StoreComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool UseDiscounts = false;
}
