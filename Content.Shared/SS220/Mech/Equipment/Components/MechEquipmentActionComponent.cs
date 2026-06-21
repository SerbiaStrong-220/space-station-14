// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Mech.Equipment.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class MechEquipmentActionComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid? EquipmentAbilityAction = null;

    /// <summary>
    /// Prototype of action this equpment provides
    /// </summary>
    [DataField]
    public EntProtoId? EquipmentAbilityActionName = null;
}
