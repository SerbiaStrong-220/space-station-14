// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

[Prototype("randomVoicesList")]
public sealed partial class RandomVoicesListPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("voices")]
    public IReadOnlyList<string> VoicesList { get; private set; } = new List<string>();
}
