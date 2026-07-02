// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed partial class DiseaseCurePoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<ReagentPrototype>> Natural = new();

    [DataField(required: true)]
    public List<ProtoId<ReagentPrototype>> Synthesized = new();

    [DataField]
    public List<ProtoId<ReagentPrototype>> Accelerants = new();
}
