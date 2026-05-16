// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Objectives.Systems;

namespace Content.Server.SS220.Objectives.Components;

/// <summary>
/// Target for cultist recruitment
/// </summary>
[RegisterComponent, Access(typeof(CultYoggEnslaveConditionSystem))]
public sealed partial class CultYoggEnslaveConditionComponent : Component
{
    /// <summary>
    /// The number of cultists and Mi-Gos required for progression
    /// </summary>
    [DataField]
    public int ReqCultFactionAmount = 6;
}
