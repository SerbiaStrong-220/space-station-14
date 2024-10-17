// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Objectives.Systems;

namespace Content.Server.SS220.Objectives;

/// <summary>
/// Handle progress of summoning
/// </summary>
[RegisterComponent, Access(typeof(CultYoggSummonConditionSystem))]
public sealed partial class CultYoggSummonConditionComponent : Component
{
    /// <summary>
    /// Amount of sacrafices required to summon a god
    /// </summary>
    [DataField]
    public int reqSacrAmount = 3;
}
