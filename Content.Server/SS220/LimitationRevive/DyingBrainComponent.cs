// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// This is used for limiting the number of defibrillator resurrections
/// </summary>
[RegisterComponent]
public sealed partial class DyingBrainComponent : Component
{
    /// <summary>
    /// Delay before target takes brain damage
    /// </summary>
    [DataField]
    public TimeSpan TimeBetweenIncidents = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Delay before target takes brain damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextIncidentTime;

    /// <summary>
    /// How much and what type of damage will be dealt
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier? Damage;
}
