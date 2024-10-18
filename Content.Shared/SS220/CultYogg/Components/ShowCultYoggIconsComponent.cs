// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;

namespace Content.Shared.SS220.CultYogg.Components;

/// <summary>
///     This component allows you to see any icons related to CultYogg.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowCultYoggIconsComponent : Component
{

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true; //isn't working when we moved it here.
    //ToDo: Discuss, should i safe it here or move icons on different component?

    /// <summary>
    /// Cultists icon
    /// </summary>
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "CultYoggFaction";
}
