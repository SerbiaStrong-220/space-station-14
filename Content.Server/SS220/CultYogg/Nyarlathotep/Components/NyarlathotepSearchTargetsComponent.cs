// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.CultYogg.Nyarlathotep;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Components;

/// <summary>
/// Periodically look for targets on surrounding objects.
/// </summary>
[RegisterComponent, Access(typeof(NyarlathotepTargetSearcherSystem)), AutoGenerateComponentPause]
public sealed partial class NyarlathotepSearchTargetsComponent : Component
{
    /// <summary>
    /// Minimum interval between searches.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SearchMinInterval = 2.5f;

    /// <summary>
    /// Maximum interval between searches.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SearchMaxInterval = 8.0f;

    /// <summary>
    /// Search target selection radius.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SearchRange = 5f;

    /// <summary>
    /// The time at which the next target search will occur.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextSearchTime;
}
