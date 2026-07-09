// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;

[Serializable, NetSerializable]
public enum VaccinatorUiKey : byte
{
    Key
}

/// <summary>Client asks vaccinator to scan inserted blood sample.</summary>
[Serializable, NetSerializable]
public sealed class VaccinatorScanMessage : BoundUserInterfaceMessage;

/// <summary>Client asks to pull trico from inserted container to buffer.</summary>
[Serializable, NetSerializable]
public sealed class VaccinatorTransferMessage : BoundUserInterfaceMessage;

/// <summary>Client asks to create a vaccine from cured blood + buffered trico.</summary>
[Serializable, NetSerializable]
public sealed class VaccinatorCreateVaccineMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VaccinatorPrintMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VaccinatorVirusResult
{
    /// <summary>Virus name, null when not every symptom is revealed.</summary>
    public string? Name;
    public List<string> Symptoms = [];
    public int UnreadableCount;

    /// <summary>Cure reagent names vaccinator read.</summary>
    public List<string> CureReagents = [];

    /// <summary>True when a cure exists but can't be read.</summary>
    public bool CureHidden;

    /// <summary>True when this virus is suppressed.</summary>
    public bool Suppressed;
}

[Serializable, NetSerializable]
public sealed class VaccinatorBoundUserInterfaceState(
    VirologyMachineStatus status,
    TimeSpan? operationEnd,
    TimeSpan operationDuration,
    List<VaccinatorVirusResult> viruses,
    float bufferTricordrazine,
    string? stationName) : BoundUserInterfaceState
{
    public readonly VirologyMachineStatus Status = status;

    /// <summary>When running scan/print finishes. Client animates bar off this.</summary>
    public readonly TimeSpan? OperationEnd = operationEnd;
    public readonly TimeSpan OperationDuration = operationDuration;

    /// <summary>One block per virus found in sample.</summary>
    public readonly List<VaccinatorVirusResult> Viruses = viruses;

    public readonly float BufferTricordrazine = bufferTricordrazine;
    public readonly string? StationName = stationName;
}
