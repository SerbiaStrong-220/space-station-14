// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ChameleonStructure;

/// <summary>
///     Allow players to change sctructure sprite to any other structure prototype.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedChameleonStructureSystem))]
public sealed partial class ChameleonStructureComponent : Component
{
    /// <summary>
    ///     EntityPrototype id that chameleon item is trying to mimic.
    ///     Can be set as default.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public EntProtoId? Prototype;

    /// <summary>
    ///     Filter possible chameleon options by a tag.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype>? RequireTag;

    /// <summary>
    ///     RequireTag alternative.
    /// </summary>
    [DataField]
    public List<EntProtoId>? ProtoList;

    /// <summary>
    ///     if we want the descendant prototypes to be usable in the chameleon, but not displayed in the UI
    /// </summary>
    [DataField]
    public bool AllowChildProto = true;

    [DataField]
    public EntityWhitelist? UserWhitelist;

    /// <summary>
    ///     Loaded list of prototypes for chameleon
    /// </summary>
    [ViewVariables]
    public List<EntProtoId> ListData = [];
}

[Serializable, NetSerializable]
public sealed class ChameleonStructureBoundUserInterfaceState(EntProtoId? selectedId, List<EntProtoId> listData, ProtoId<TagPrototype>? requiredTag) : BoundUserInterfaceState
{
    public readonly EntProtoId? SelectedId = selectedId;
    public readonly ProtoId<TagPrototype>? RequiredTag = requiredTag;
    public readonly List<EntProtoId> ListData = listData;
}

[Serializable, NetSerializable]
public sealed class ChameleonStructurePrototypeSelectedMessage(EntProtoId selectedId) : BoundUserInterfaceMessage
{
    public readonly EntProtoId SelectedId = selectedId;
}

[Serializable, NetSerializable]
public enum ChameleonStructureUiKey : byte
{
    Key
}
