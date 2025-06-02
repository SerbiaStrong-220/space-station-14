// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.Sacraficials;

/// <summary>
/// Used to markup cult's sacrifice targets
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultYoggSacrificialComponent : Component
{
    [ViewVariables]
    public bool IconVisibleToGhost = true;

    [ViewVariables]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "CultYoggSacraficialTargetIcon";

    /// <summary>
    /// Tier number required for replacement if it needed
    /// </summary>
    [ViewVariables]
    public int Tier = 0;//initilize as max possible tier

    /// <summary>
    /// Time required for announcement
    /// </summary>
    [ViewVariables]
    public TimeSpan AnnounceReplacementCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Time required for replacement
    /// </summary>
    [ViewVariables]
    public TimeSpan ReplacementCooldown = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Time penalty if sacrificial decides to commit suicide
    /// </summary>
    [ViewVariables]
    public TimeSpan SuicidePenaltyTime = TimeSpan.FromSeconds(300);

    [ViewVariables]
    public bool WasSacraficed = false;
}
