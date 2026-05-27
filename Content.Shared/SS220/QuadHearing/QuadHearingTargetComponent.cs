using Robust.Shared.GameStates;

namespace Content.Shared.SS220.QuadHearing;

[RegisterComponent, NetworkedComponent]
public sealed partial class QuadHearingTargetComponent : Component
{
    [DataField]
    public Color Color = Color.LightBlue;
}
