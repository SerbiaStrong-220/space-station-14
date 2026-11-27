// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Power;

/// <summary>
/// Repairable controller faults, independent of Malf AI ownership and CPU generation.
/// </summary>
[RegisterComponent]
public sealed partial class PowerMalfunctionComponent : Component
{
    [DataField]
    public bool Faulty;

    [DataField]
    public bool PowerDisabled;

    [DataField]
    public bool PreviouslyEnabled;

    [DataField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public HashSet<ProtoId<StackPrototype>> RequiredCables = new();

    [DataField]
    public HashSet<ProtoId<StackPrototype>> InsertedCables = new();

    /// <summary>
    /// Invalidates an in-progress repair if a new fault or hack occurs.
    /// </summary>
    [DataField]
    public int Revision;
}
