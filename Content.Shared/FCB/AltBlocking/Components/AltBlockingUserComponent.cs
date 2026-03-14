// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.FCB.AltBlocking;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class AltBlockingUserComponent : Component
{
    /// <summary>
    /// The entities that's being used to block and are shields
    /// </summary>
    [DataField("blockingItemsShields")]
    public List<EntityUid?> BlockingItemsShields = new();

    [DataField, AutoNetworkedField]
    public bool IsBlocking = false;

    [DataField]
    public EntProtoId BlockingToggleAction = "ActionToggleBlock";

    [DataField]
    public EntityUid? BlockingToggleActionEntity;
}
