using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.Prototype;

[Prototype("narsiAbilityPrototype")]
public sealed class NarsiAbilityPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true, serverOnly: true)]
    public string Name = default!;

    [DataField("description", required: true, serverOnly: true)]
    public string Description = default!;

    [DataField("level1Description", required: true, serverOnly: true)]
    public string Level1Description = default!;

    [DataField("level2Description", required: true, serverOnly: true)]
    public string Level2Description = default!;

    [DataField("level3Description", required: true, serverOnly: true)]
    public string Level3Description = default!;

    [DataField("actionId", required: true, serverOnly: true)]
    public EntProtoId ActionId;

    [DataField("icon", required: true, serverOnly: true)]
    public SpriteSpecifier Icon = default!;

    [DataField("requiredBloodScore", required: true, serverOnly: true)]
    public int RequiredBloodScore;
}
