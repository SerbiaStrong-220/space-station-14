// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ClinkGlasses;

/// <summary>
/// Used for entities that can be clinked with other ones.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class ClinkGlassesComponent : Component
{
    [DataField]
    public SoundSpecifier SoundOnComplete = new SoundPathSpecifier("/Audio/SS220/Effects/clink_glasses.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.15f)
    };
}
