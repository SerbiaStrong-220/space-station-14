using Content.Shared.Whitelist;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;

[DataDefinition]
public sealed partial class NarsiRitualRequirements
{
    [DataField(required: true)]
    public int CultistsCount;

    [DataField]
    public float EntitiesFoundingRange = 3f;

    [DataField]
    public NarsiRitualBloodPuddleRequirements? BloodPuddleRequirements;

    [DataField]
    public List<NarsiRitualRequirementsEntity>? EntitiesRequirements;

    [DataField]
    public EntityWhitelist? BuckledEntityWhitelist;
}

[DataDefinition]
public sealed partial class NarsiRitualRequirementsEntity
{
    [DataField]
    public string Name = default!;

    [DataField]
    public int Count;

    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;
}

[DataDefinition]
public sealed partial class NarsiRitualBloodPuddleRequirements
{
    [DataField]
    public int Count;

    [DataField(required: true)]
    public List<string> ReagentsWhitelist = default!;
}
