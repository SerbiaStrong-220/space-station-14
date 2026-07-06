// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;

[Serializable, NetSerializable]
public enum VirusDiagnoserUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class VirusDiagnoserScanMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VirusDiagnoserTransferMutagenMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VirusDiagnoserCopyMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VirusDiagnoserPrintMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VirusDiagnoserResult
{
    /// <summary>Strain name, null if not every symptom is readable.</summary>
    public string? Name;

    /// <summary>Descriptions of symptoms diagnoser could read.</summary>
    public List<string> Symptoms = [];

    /// <summary>How many symptoms could not be decoded.</summary>
    public int UnreadableCount;

    /// <summary>Spread vectors, filled only once every symptom has been revealed.</summary>
    public VirusTransmissionVector Transmission;
}

[Serializable, NetSerializable]
public sealed class VirusDiagnoserBoundUserInterfaceState(
    bool hasSample,
    bool scanning,
    bool printing,
    TimeSpan? operationEnd,
    TimeSpan operationDuration,
    bool hasResult,
    List<VirusDiagnoserResult> viruses,
    float bufferMutagen,
    string? stationName) : BoundUserInterfaceState
{
    public readonly bool HasSample = hasSample;
    public readonly bool Scanning = scanning;
    public readonly bool Printing = printing;
    public readonly TimeSpan? OperationEnd = operationEnd;
    public readonly TimeSpan OperationDuration = operationDuration;
    public readonly bool HasResult = hasResult;
    public readonly List<VirusDiagnoserResult> Viruses = viruses;
    public readonly float BufferMutagen = bufferMutagen;
    public readonly string? StationName = stationName;
}
