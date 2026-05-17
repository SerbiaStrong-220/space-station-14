using Robust.Shared.GameStates;

namespace Content.Shared.SS220.FourChannelHearing;

[RegisterComponent, NetworkedComponent]
public sealed partial class FourChannelHearingTargetComponent : Component
{
    [DataField]
    public Color Color = Color.LightBlue;
}
