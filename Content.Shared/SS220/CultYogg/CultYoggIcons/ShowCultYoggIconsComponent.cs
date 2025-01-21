// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.CultYoggIcons;

/// <summary>
///     This component allows you to see any icons related to CultYogg.
///     Made this component becase icons must be visible to cultists, cult animals and migo
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
    public ProtoId<FactionIconPrototype> StatusIcon = "CultYoggFactionIcon";
}
