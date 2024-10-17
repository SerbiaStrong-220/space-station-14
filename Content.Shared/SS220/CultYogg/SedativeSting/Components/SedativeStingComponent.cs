using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CultYogg.SedativeSting.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SedativeStingComponent : Component
{
    [DataField]
    public string SolutionName = "hypospray";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    [DataField]
    public bool IgnoreProtection = false;
}
