// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Alert;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.AltBlocking;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class AltBlockingUserComponent : Component
{
    /// <summary>
    /// The entities that's being used to block and are shields
    /// </summary>
    [AutoNetworkedField]
    public List<EntityUid> BlockingItemsShields = new();

    [DataField, AutoNetworkedField]
    public bool Blocking = false;

    [DataField]
    public ProtoId<AlertPrototype> BlockingAlertProtoId = "ActiveBlocking";

    [DataField]
    public ProtoId<BlockingIconPrototype> Icon = "BlockingIcon";
}
