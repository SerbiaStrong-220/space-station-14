using Content.Shared.Storage;

namespace Content.Server.SS220.StationEvents;

[RegisterComponent, Access(typeof(CableRandomSpawnRule))]
public sealed partial class CableRandomSpawnRuleComponent : Component
{
    [DataField("entries")]
    public List<EntitySpawnEntry> Entries = new();

    /// <summary>
    /// At least one special entry is guaranteed to spawn
    /// </summary>
    [DataField("specialEntries")]
    public List<EntitySpawnEntry> SpecialEntries = new();
}
