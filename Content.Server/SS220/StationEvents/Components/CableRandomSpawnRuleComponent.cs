// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.SS220.StationEvents.Components;

[RegisterComponent, Access(typeof(CableRandomSpawnRule))]
public sealed partial class CableRandomSpawnRuleComponent : Component
{
    [DataField]
    public List<EntitySpawnEntry> Entries = [];

    /// <summary>
    /// At least one special entry is guaranteed to spawn
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> SpecialEntries = [];
}
