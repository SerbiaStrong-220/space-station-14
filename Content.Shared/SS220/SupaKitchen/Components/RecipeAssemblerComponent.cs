using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SupaKitchen.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RecipeAssemblerComponent : Component
{
    [DataField]
    public float Range = 1f;
}
