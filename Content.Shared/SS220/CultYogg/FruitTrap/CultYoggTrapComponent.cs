// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Trap;
using Content.Shared.Stealth.Components;

namespace Content.Shared.SS220.CultYogg.FruitTrap;

/// <summary>
/// This is used for Modified <see cref="TrapSystem"/> for cult traps. All modifications add additional conditions.
/// </summary>
[RegisterComponent]
public sealed partial class CultYoggTrapComponent : Component
{
    /// <summary>
    /// Maximum number of simultaneously armed traps.
    /// If -1, there will be no limit on the number of traps.
    /// </summary>
    [DataField]
    public int TrapsLimit = -1;

    /// <summary>
    /// Value visibility <see cref="StealthComponent"/> on armed trap
    /// </summary>
    [DataField]
    public float ArmedVisibility;
    /// <summary>
    /// Value visibility <see cref="StealthComponent"/> on un armed trap
    /// </summary>
    [DataField]
    public float UnArmedVisibility;
}
