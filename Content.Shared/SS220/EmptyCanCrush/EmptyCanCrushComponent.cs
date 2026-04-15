using Robust.Shared.Audio;
namespace Content.Shared.SS220.EmptyCanCrush;
[RegisterComponent]
public sealed partial class EmptyCanCrushComponent : Component
{
    [DataField("CrushedCan")]
    public string CrushedCanId = "CrushedCanCola";

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
