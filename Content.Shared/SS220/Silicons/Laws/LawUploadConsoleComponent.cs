// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Silicons.Laws;

[RegisterComponent, NetworkedComponent]
public sealed partial class LawUploadConsoleComponent : Component
{
    public const string CardSlot = "law_upload_id";
    public const string BoardSlot = "circuit_holder";

    public int Revision;
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
    int revision,
    string status,
    bool hasCard,
    bool hasBoard,
    bool canApply,
    string aiName,
    string[] aiLaws,
    string borgName,
    string[] borgLaws,
    string boardName,
    string[] boardLaws) : BoundUserInterfaceState
{
    public readonly int Revision = revision;
    public readonly string Status = status;
    public readonly bool HasCard = hasCard;
    public readonly bool HasBoard = hasBoard;
    public readonly bool CanApply = canApply;
    public readonly string AiName = aiName;
    public readonly string[] AiLaws = aiLaws;
    public readonly string BorgName = borgName;
    public readonly string[] BorgLaws = borgLaws;
    public readonly string BoardName = boardName;
    public readonly string[] BoardLaws = boardLaws;
}

[Serializable, NetSerializable]
public sealed class ApplyStationLawsMessage(LawUploadTarget target, int revision) : BoundUserInterfaceMessage
{
    public readonly LawUploadTarget Target = target;
    public readonly int Revision = revision;
}

/// <summary>
/// Refreshes upload consoles after a station default changes.
/// </summary>
public sealed class StationLawsetsChangedEvent(EntityUid station) : EntityEventArgs
{
    public readonly EntityUid Station = station;
}
