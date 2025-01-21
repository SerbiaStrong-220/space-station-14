using Content.Server.SS220.StationEvents.Events;
using Content.Server.SS220.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.SS220.StationEvents.Components;

[RegisterComponent, Access(typeof(RegalRatRule))]
public sealed partial class RegalRatRuleComponent : Component
{
    [DataField("entries")]
    public List<EntitySpawnEntry> Entries = new();

    /// <summary>
    /// At least one special entry is guaranteed to spawn
    /// </summary>
    [DataField("specialEntries")]
    public List<EntitySpawnEntry> SpecialEntries = new();
}
