// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Felinids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DodgeComponent : Component
{
    /// <summary>
    ///     Base chance to dodge bullet or hitscan.
    /// </summary>
    [DataField]
    public float BaseDodgeChance = 0.18f;

    /// <summary>
    ///     Multiplier of the effect of damage on the chance of dodge.
    /// </summary>
    [DataField]
    public float DamageAffect = 0.35f;

    /// <summary>
    ///     Multiplier of the effect of hunger on the chance of dodge.
    /// </summary>
    [DataField]
    public float HungerAffect = 0.8f;

    /// <summary>
    ///     Multiplier of the effect of thirst on the chance of dodge.
    /// </summary>
    [DataField]
    public float ThirstAffect = 0.8f;

    public List<EntityUid> EntityWhitelist = [];
}
