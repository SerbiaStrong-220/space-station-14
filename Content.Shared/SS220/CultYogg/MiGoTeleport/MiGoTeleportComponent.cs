// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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

    [DataField, AutoNetworkedField]
    public TimeSpan TeleportCooldown = TimeSpan.FromSeconds(1);//To avoid spamming

    [ViewVariables]
    public TimeSpan? NextTeleportAvaliable;
}

[Serializable, NetSerializable]
public sealed class MiGoTeleportBuiState : BoundUserInterfaceState
{
    public List<(string, NetEntity)> Warps = [];
}

[Serializable, NetSerializable]
public enum MiGoTeleportUiKey : byte
{
    Teleport
}
