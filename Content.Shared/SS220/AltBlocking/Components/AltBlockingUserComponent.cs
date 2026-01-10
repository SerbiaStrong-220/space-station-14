// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.AltBlocking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class AltBlockingUserComponent : Component
{
    /// <summary>
    /// The entities that's being used to block and are shields
    /// </summary>
    [DataField("blockingItemsShields")]
    public List<EntityUid?> BlockingItemsShields = new();

    [AutoNetworkedField]
    public int randomSeed = 0;//This is NOT for prototyping

    [DataField, AutoNetworkedField]
    public bool IsBlocking = false;

    [DataField]
    public EntProtoId BlockingToggleAction = "ActionToggleBlock";

    [DataField]
    public EntityUid? BlockingToggleActionEntity;
}
