// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology;

[Prototype("virusStrain")]
public sealed partial class VirusPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Default name of virus. A mutated strain carry a generated name instead.</summary>
    [DataField]
    public LocId? Name;

    /// <summary>Symptoms virus is made of.</summary>
    [DataField(required: true)]
    public List<ProtoId<VirusSymptomPrototype>> Symptoms = [];

    /// <summary>How this virus spreads. Null = can't.</summary>
    [DataField]
    public VirusTransmission? Transmission;
}
