// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

public interface IHearableChannelPrototype : IPrototype
{
    string ID { get; }
    string LocalizedName { get; }
    Color Color { get; }
}
