// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
    ///     Сan be set as default.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public EntProtoId? Prototype;

    /// <summary>
    ///     Filter possible chameleon options by a tag in addition to WhitelistChameleon.
    /// </summary>
    [DataField]
    public string? RequireTag;

    [DataField]
    public EntityWhitelist? UserWhitelist;
}

[Serializable, NetSerializable]
public sealed class ChameleonStructureBoundUserInterfaceState(string? selectedId, string? requiredTag) : BoundUserInterfaceState
{
    public readonly string? SelectedId = selectedId;
    public readonly string? RequiredTag = requiredTag;
}

[Serializable, NetSerializable]
public sealed class ChameleonStructurePrototypeSelectedMessage(string selectedId) : BoundUserInterfaceMessage
{
    public readonly string SelectedId = selectedId;
}

[Serializable, NetSerializable]
public enum ChameleonStructureUiKey : byte
{
    Key
}
