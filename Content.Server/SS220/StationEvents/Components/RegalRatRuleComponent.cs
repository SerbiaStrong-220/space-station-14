// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.SS220.StationEvents.Components;

[RegisterComponent, Access(typeof(RegalRatRule))]
public sealed partial class RegalRatRuleComponent : Component
{
    [DataField]
    public List<EntitySpawnEntry> Entries = new();

    [DataField]
    public List<EntitySpawnEntry> SpecialEntries = new();
}
