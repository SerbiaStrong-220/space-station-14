// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.FCB.ToggleBlocking;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleBlockingChanceComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet? OriginalActiveModifier;
    [DataField, AutoNetworkedField]
    public bool IsToggled = false;

    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet? OriginalPassiveModifier;
    [DataField, AutoNetworkedField]
    public float ToggledRangeBlockProb = 0.5f;

    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet? DeactivatedActiveModifier;
    [DataField, AutoNetworkedField]
    public float BaseRangeBlockProb = 0f;

    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet? DeactivatedPassiveModifier;
    [DataField, AutoNetworkedField]
    public float ToggledMeleeBlockProb = 0.5f;

    [DataField, AutoNetworkedField]
    public float BaseMeleeBlockProb = 0f;
}
