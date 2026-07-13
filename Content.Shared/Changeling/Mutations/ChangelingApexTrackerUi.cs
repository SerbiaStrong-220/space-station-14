// SS220 changeling Apex tracker
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Mutations;

[Serializable, NetSerializable]
public enum ChangelingApexTrackerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ChangelingApexTargetSelectedMessage(uint selectionToken) : BoundUserInterfaceMessage
{
    /// <summary>
    /// Opaque, short-lived server-issued token. This must never be an entity identifier.
    /// </summary>
    public uint SelectionToken { get; } = selectionToken;
}

[Serializable, NetSerializable]
public sealed class ChangelingApexTrackerUiState(List<ChangelingApexTargetEntry> targets) : BoundUserInterfaceState
{
    public List<ChangelingApexTargetEntry> Targets { get; } = targets;
}

[Serializable, NetSerializable]
public readonly record struct ChangelingApexTargetEntry(
    uint SelectionToken,
    string Name,
    ProtoId<JobIconPrototype>? JobIcon);
