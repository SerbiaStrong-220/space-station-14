// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CultMiniMapComponent : Component
{
    [ViewVariables]
    public EntProtoId MiniMapAction = "ActionCultMiniMap";

    [ViewVariables, AutoNetworkedField]
    public EntityUid? MiniMapActionEntity;
}

public sealed partial class CultMiniMapActionEvent : InstantActionEvent
{
}
