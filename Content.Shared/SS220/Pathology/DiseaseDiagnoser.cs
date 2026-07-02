// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[Serializable, NetSerializable]
public enum DiseaseDiagnoserUiKey : byte
{
    Key
}

/// <summary>
/// Sent by the client to ask the diagnoser to scan the inserted sample.
/// </summary>
[Serializable, NetSerializable]
public sealed class DiseaseDiagnoserScanMessage : BoundUserInterfaceMessage;

/// <summary>
/// Sent by the client to pull stable mutagen out of the inserted container into the machine buffer.
/// </summary>
[Serializable, NetSerializable]
public sealed class DiseaseDiagnoserTransferMutagenMessage : BoundUserInterfaceMessage;

/// <summary>
/// Sent by the client to transcribe the sample's virus into a fresh bottle, spending buffer mutagen.
/// </summary>
[Serializable, NetSerializable]
public sealed class DiseaseDiagnoserCopyMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DiseaseDiagnoserPrintMessage : BoundUserInterfaceMessage;

/// <summary>One analysed virus from the sample — its own name, symptoms and spread vectors.</summary>
[Serializable, NetSerializable]
public sealed class DiseaseDiagnoserVirusResult
{
    /// <summary>Virus name, null if not every symptom is readable.</summary>
    public string? Name;

    /// <summary>Descriptions of symptoms diagnoser could read.</summary>
    public List<string> Symptoms = new();

    /// <summary>How many symptoms could not be decoded.</summary>
    public int UnreadableCount;

    /// <summary>Spread vectors, filled only once every symptom of the strain has been revealed.</summary>
    public VirusTransmissionVector Transmission;
}

[Serializable, NetSerializable]
public sealed class DiseaseDiagnoserBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool HasSample;
    public readonly bool Scanning;
    public readonly bool Printing;

    /// <summary>When the running scan/print finishes. Client animates bar off this.</summary>
    public readonly TimeSpan? OperationEnd;
    public readonly TimeSpan OperationDuration;

    public readonly bool HasResult;

    /// <summary>One block per virus found in sample.</summary>
    public readonly List<DiseaseDiagnoserVirusResult> Viruses;

    /// <summary>Stable mutagen currently held in buffer.</summary>
    public readonly float BufferMutagen;
    public readonly string? StationName;

    public DiseaseDiagnoserBoundUserInterfaceState(
        bool hasSample,
        bool scanning,
        bool printing,
        TimeSpan? operationEnd,
        TimeSpan operationDuration,
        bool hasResult,
        List<DiseaseDiagnoserVirusResult> viruses,
        float bufferMutagen,
        string? stationName)
    {
        HasSample = hasSample;
        Scanning = scanning;
        Printing = printing;
        OperationEnd = operationEnd;
        OperationDuration = operationDuration;
        HasResult = hasResult;
        Viruses = viruses;
        BufferMutagen = bufferMutagen;
        StationName = stationName;
    }
}
