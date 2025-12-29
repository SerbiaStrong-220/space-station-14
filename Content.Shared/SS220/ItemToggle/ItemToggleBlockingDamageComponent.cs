using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ItemToggle;

/// <summary>
/// This is used for changing blocking damage while item not activated
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ItemToggleBlockingDamageComponent : Component
{
//SS220 shield rework begin
    [DataField, AutoNetworkedField]
    public bool IsToggled = false;

    [DataField, AutoNetworkedField]
    public float ToggledRangeBlockProb = 0.5f;

    [DataField, AutoNetworkedField]
    public float BaseRangeBlockProb = 0f;

    [DataField, AutoNetworkedField]
    public float ToggledMeleeBlockProb = 0.5f;

    [DataField, AutoNetworkedField]
    public float BaseMeleeBlockProb = 0f;
//SS220 shield rework end
}
