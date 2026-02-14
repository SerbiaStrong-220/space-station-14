using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TraitorDynamics;

[Prototype]
public sealed partial class DynamicPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId Name;

    /// <summary>
    /// Dictionary that defines maximum player counts for specific gamerules.
    /// Key - the list of the pref roles.
    /// Value - maximum number of players allowed for this rule.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagPrototype>, int> AntagLimits = new();

    [DataField]
    public int PlayersRequirement;

    /// <summary>
    /// Minimum players in department required to add this dynamic to dynamics random pool <br/>
    /// if it states 5 than if players 5 or more it will be added
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DepartmentPrototype>, int> DepartmentLimits = new();

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? LoreNames;

    [DataField]
    public LocId? Briefing;

    public LocId? SelectedLoreName;
}
