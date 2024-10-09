using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Prototypes;

[Prototype]
public sealed class NarsiRitualCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true, serverOnly: true)]
    public readonly string Name = default!;

    [DataField(required: true, serverOnly: true)]
    public List<ProtoId<NarsiRitualPrototype>> Rituals = new();
}
