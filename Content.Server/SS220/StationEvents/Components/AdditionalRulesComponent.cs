// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.SS220.StationEvents.Components;

/// <summary>
/// A component when you want to add a table of other events along with this one
/// </summary>
[RegisterComponent]
public sealed partial class AdditionalRulesComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, EntityTableSelector> Rules = [];
}
