// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Medical;

namespace Content.Server.SS220.DefibrillatorSkill;

/// <summary>
/// This is used to determine the whitelist, those who know how to use a defibrillator <see cref="DefibrillatorSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class DefibrillatorSkillComponent : Component
{
    /// <summary>
    /// Chance of a successful shock with a defibrillator to revive a corpse.
    /// </summary>
    [DataField]
    public float ChanceWithMedSkill = 0.99f; // Very rare event. Must be other way to fix defibspam
}
