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
            Params = AudioParams.Default.WithVariation(1.5f)
        };

}
