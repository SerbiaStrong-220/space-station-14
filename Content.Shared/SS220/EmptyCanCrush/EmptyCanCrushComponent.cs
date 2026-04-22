//© SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EmptyCanCrush;

[RegisterComponent]
public sealed partial class EmptyCanCrushComponent : Component
{
    [DataField("crushedCan")]
    public EntProtoId CrushedCanId = "crushedCanCola";

    [DataField]
    public SoundSpecifier CrushSound =
        new SoundPathSpecifier("/Audio/SS220/Effects/can_crush.ogg")
        {
            Params = AudioParams.Default
                .WithVolume(0.2f)
                .WithMaxDistance(5f)
                .WithRolloffFactor(1f)
        };
}
