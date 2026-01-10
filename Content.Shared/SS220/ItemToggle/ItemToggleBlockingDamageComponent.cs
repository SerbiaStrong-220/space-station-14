using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ItemToggle;

/// <summary>
/// This is used for changing blocking probabilities when blocking item is activated(active block with not activated item is not possible)
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ItemToggleBlockingDamageComponent : Component
{
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
}
