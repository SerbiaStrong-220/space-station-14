// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Silicons.Laws;

namespace Content.Shared.SS220.Silicons.Laws;

[RegisterComponent, NetworkedComponent]
public sealed partial class LawUploadConsoleComponent : Component
{
    public const string CardSlot = "law_upload_id";
    public const string BoardSlot = "circuit_holder";

}

[Serializable, NetSerializable]
public enum LawUploadUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum LawUploadTarget : byte
{
    Ai,
    Borgs,
    All,
}

[Serializable, NetSerializable]
public sealed class LawUploadState(
    NetEntity? board,
    string status,
    bool hasCard,
    bool hasBoard,
    bool canApply,
    ProtoId<SiliconLawsetPrototype> aiLawset,
    ProtoId<SiliconLawPrototype>[] aiLaws,
    ProtoId<SiliconLawsetPrototype> borgLawset,
    ProtoId<SiliconLawPrototype>[] borgLaws,
    ProtoId<SiliconLawsetPrototype> boardLawset,
    ProtoId<SiliconLawPrototype>[] boardLaws) : BoundUserInterfaceState
{
    public readonly NetEntity? Board = board;
    public readonly string Status = status;
    public readonly bool HasCard = hasCard;
    public readonly bool HasBoard = hasBoard;
    public readonly bool CanApply = canApply;
    public readonly ProtoId<SiliconLawsetPrototype> AiLawset = aiLawset;
    public readonly ProtoId<SiliconLawPrototype>[] AiLaws = aiLaws;
    public readonly ProtoId<SiliconLawsetPrototype> BorgLawset = borgLawset;
    public readonly ProtoId<SiliconLawPrototype>[] BorgLaws = borgLaws;
    public readonly ProtoId<SiliconLawsetPrototype> BoardLawset = boardLawset;
    public readonly ProtoId<SiliconLawPrototype>[] BoardLaws = boardLaws;
}

[Serializable, NetSerializable]
public sealed class ApplyStationLawsMessage(LawUploadTarget target, NetEntity board) : BoundUserInterfaceMessage
{
    public readonly LawUploadTarget Target = target;
    public readonly NetEntity Board = board;
}

/// <summary>
/// Refreshes upload consoles after a station default changes.
/// </summary>
public sealed class StationLawsetsChangedEvent(EntityUid station) : EntityEventArgs
{
    public readonly EntityUid Station = station;
}
