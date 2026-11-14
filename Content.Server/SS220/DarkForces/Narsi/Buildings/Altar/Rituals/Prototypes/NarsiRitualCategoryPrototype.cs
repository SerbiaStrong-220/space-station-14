using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Prototypes;

[Prototype]
public sealed partial class NarsiRitualCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true, serverOnly: true)]
    public string Name = default!;

    [DataField(required: true, serverOnly: true)]
    public List<ProtoId<NarsiRitualPrototype>> Rituals = new();
}
