// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
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
