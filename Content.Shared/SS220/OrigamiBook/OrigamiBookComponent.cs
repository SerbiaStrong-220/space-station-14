using Robust.Shared.GameStates;

namespace Content.Shared.SS220.OrigamiBook;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class OrigamiBookComponent : Component
{
    [DataField]
    public float DelayToLearn;

    [DataField]
    public float ChanceToLearn;
}
