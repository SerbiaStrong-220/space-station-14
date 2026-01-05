using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Shared.Blocking;

/// <summary>
/// This component gets dynamically added to an Entity via the <see cref="BlockingSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlockingUserComponent : Component
{
    /// <summary>
    /// The entities that's being used to block and are shields
    /// </summary>
    [DataField("blockingItemsShields")]
    public List<EntityUid?> BlockingItemsShields = new();

    [DataField, AutoNetworkedField]
    public int randomSeed = 0;//This is NOT for prototyping
    //
    [DataField, AutoNetworkedField]
    public bool IsBlocking = false;

    [DataField]
    public EntProtoId BlockingToggleAction = "ActionToggleBlock";

    [DataField]
    public EntityUid? BlockingToggleActionEntity;
    /// <summary>
    /// The entity that's being used to block
    /// </summary>
    //[DataField("blockingItem")]
    //public EntityUid? BlockingItem;

    /// <summary>
    /// Stores the entities original bodytype
    /// Used so that it can be put back to what it was after anchoring
    /// </summary>
    //[DataField("originalBodyType")]
    //public BodyType OriginalBodyType;
}
