using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SupaKitchen.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class RecipeAssemblerComponent : BaseCookingInstrumentComponent
{
    [AutoNetworkedField]
    public HashSet<EntityUid> Entities = [];
}
