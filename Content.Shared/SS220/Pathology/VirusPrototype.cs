// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed partial class VirusPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Default name of the virus. Player may rename a composed strain. </summary>
    [DataField]
    public LocId? Name;

    /// <summary>Symptoms the virus is made of.</summary>
    [DataField(required: true)]
    public List<ProtoId<PathologyPrototype>> Symptoms = new();

    /// <summary>How this virus spreads. Null = can't transmit on its own.</summary>
    [DataField]
    public VirusTransmission? Transmission;
}
