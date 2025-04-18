using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;

namespace Content.Shared.SS220.Clothing;

/// <summary>
///     This component gives an item an action that will equip or un-equip some clothing e.g. hardsuits and hardsuit helmets.
/// </summary>
[RegisterComponent, NetworkedComponent/*, AutoGenerateComponentState*/]
public sealed partial class InnerToggleableClothingComponent : Component
{
    /*
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> Action = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    */
}
