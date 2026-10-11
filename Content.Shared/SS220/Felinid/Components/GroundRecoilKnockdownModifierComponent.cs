using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Felinid.Components;

/// <summary>
/// Clothing that scales the chance of being knocked down by ground recoil while worn.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GroundRecoilKnockdownModifierComponent : Component
{
    [DataField]
    public float Multiplier = 1f;
}
