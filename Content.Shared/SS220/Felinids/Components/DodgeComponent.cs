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
    public float BaseDodgeChance = 0.15f;
}
