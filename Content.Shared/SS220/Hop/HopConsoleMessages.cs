// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Hop;

[Serializable, NetSerializable]
public enum HopConsoleUiKey : byte { Key }

[Serializable, NetSerializable]
public enum HopConsoleOperation : byte { SaveJob, AddRecord, RemoveRecord }

[Serializable, NetSerializable]
public sealed class HopConsoleRefreshMessage : BoundUserInterfaceMessage;

/// <summary>One atomic update, with the last observed values to prevent overwriting a late join.</summary>
[Serializable, NetSerializable]
public sealed class HopConsoleSaveJobMessage(
    string jobId, int? slots, bool priority, int? expectedSlots, bool expectedPriority) : BoundUserInterfaceMessage
{
    public readonly string JobId = jobId;
    public readonly int? Slots = slots;
    public readonly bool Priority = priority;
    public readonly int? ExpectedSlots = expectedSlots;
    public readonly bool ExpectedPriority = expectedPriority;
}

[Serializable, NetSerializable]
public sealed class HopConsoleAddRecordMessage(string name, string jobId, string jobTitle) : BoundUserInterfaceMessage
{
    public readonly string Name = name;
    public readonly string JobId = jobId;
    public readonly string JobTitle = jobTitle;
}

[Serializable, NetSerializable]
public sealed class HopConsoleRemoveRecordMessage(uint key) : BoundUserInterfaceMessage
{
    public readonly uint Key = key;
}

[Serializable, NetSerializable]
public sealed class HopConsoleResultMessage(
    HopConsoleOperation operation, bool success, string text, HopConsoleState state) : BoundUserInterfaceMessage
{
    public readonly HopConsoleOperation Operation = operation;
    public readonly bool Success = success;
    public readonly string Text = text;
    public readonly HopConsoleState State = state;
}

[Serializable, NetSerializable]
public sealed class HopConsoleState(
    string stationName, List<HopJobEntry> jobs, List<HopRecordEntry> records,
    bool available = true, bool debugJobs = false) : BoundUserInterfaceState
{
    public readonly string StationName = stationName;
    public readonly List<HopJobEntry> Jobs = jobs;
    public readonly List<HopRecordEntry> Records = records;
    public readonly bool Available = available;
    public readonly bool DebugJobs = debugJobs;
}

[Serializable, NetSerializable]
public sealed class HopJobEntry(
    string id, string name, int? availableSlots, bool priority, int? roundStartSlots, int? configuredSlots,
    int occupiedSlots = 0, bool slotsLocked = false, bool priorityLocked = false)
{
    public readonly string Id = id;
    public readonly string Name = name;
    public readonly int? AvailableSlots = availableSlots;
    public readonly bool Priority = priority;
    public readonly int? RoundStartSlots = roundStartSlots;
    public readonly int? ConfiguredSlots = configuredSlots;
    public readonly int OccupiedSlots = occupiedSlots;
    public readonly bool SlotsLocked = slotsLocked;
    public readonly bool PriorityLocked = priorityLocked;
    public int MaximumAvailable => HopJobPolicy.MaximumAvailable(ConfiguredSlots, OccupiedSlots);
}

[Serializable, NetSerializable]
public sealed class HopRecordEntry(uint key, string name, string jobTitle, string jobId)
{
    public readonly uint Key = key;
    public readonly string Name = name;
    public readonly string JobTitle = jobTitle;
    public readonly string JobId = jobId;
}
