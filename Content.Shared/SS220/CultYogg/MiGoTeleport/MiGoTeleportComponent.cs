// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.MiGoTeleport;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MiGoTeleportComponent : Component
{
    [DataField]
    public EntProtoId TeleportAction = "ActionMiGoTeleport";

    [ViewVariables, AutoNetworkedField]
    public EntityUid? TeleportActionEntity;
}

[Serializable, NetSerializable]
public sealed class MiGoTeleportBuiState : BoundUserInterfaceState
{
    public List<(string, NetEntity)> Warps = [];
}

[Serializable, NetSerializable]
public enum MiGoTeleportUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MiGoTeleportToTargetMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public NetEntity Target = target;
}
