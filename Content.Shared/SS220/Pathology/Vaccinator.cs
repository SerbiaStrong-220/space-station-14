// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[Serializable, NetSerializable]
public enum VaccinatorUiKey : byte
{
    Key
}

/// <summary>Client asks the vaccinator to scan the inserted blood sample.</summary>
[Serializable, NetSerializable]
public sealed class VaccinatorScanMessage : BoundUserInterfaceMessage;

/// <summary>Client asks to pull trico out of the inserted container into the buffer.</summary>
[Serializable, NetSerializable]
public sealed class VaccinatorTransferMessage : BoundUserInterfaceMessage;

/// <summary>Client asks to create a vaccine from the cured blood + buffered trico.</summary>
[Serializable, NetSerializable]
public sealed class VaccinatorCreateVaccineMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VaccinatorPrintMessage : BoundUserInterfaceMessage;

/// <summary>One analysed virus from sample — its own name, symptoms, cure and suppression state.</summary>
[Serializable, NetSerializable]
public sealed class VaccinatorVirusResult
{
    /// <summary>Virus name, null when not every symptom is readable.</summary>
    public string? Name;
    public List<string> Symptoms = new();
    public int UnreadableCount;

    /// <summary>Cure reagent names vaccinator read.</summary>
    public List<string> CureReagents = new();

    /// <summary>True when a cure exists but can't be read.</summary>
    public bool CureHidden;

    /// <summary>True when this virus is suppressed.</summary>
    public bool Suppressed;
}

[Serializable, NetSerializable]
public sealed class VaccinatorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool HasSample;
    public readonly bool Scanning;
    public readonly bool Printing;

    /// <summary>When running scan/print finishes. Client animates bar off this.</summary>
    public readonly TimeSpan? OperationEnd;
    public readonly TimeSpan OperationDuration;

    public readonly bool HasResult;

    /// <summary>One block per virus found in sample.</summary>
    public readonly List<VaccinatorVirusResult> Viruses;

    public readonly float BufferTricordrazine;
    public readonly string? StationName;

    public VaccinatorBoundUserInterfaceState(
        bool hasSample,
        bool scanning,
        bool printing,
        TimeSpan? operationEnd,
        TimeSpan operationDuration,
        bool hasResult,
        List<VaccinatorVirusResult> viruses,
        float bufferTricordrazine,
        string? stationName)
    {
        HasSample = hasSample;
        Scanning = scanning;
        Printing = printing;
        OperationEnd = operationEnd;
        OperationDuration = operationDuration;
        HasResult = hasResult;
        Viruses = viruses;
        BufferTricordrazine = bufferTricordrazine;
        StationName = stationName;
    }
}
