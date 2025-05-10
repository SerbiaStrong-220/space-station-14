using Robust.Shared.GameStates;

namespace Content.Shared.SS220.OrigamiBook;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class OrigamiUserComponent : Component
{
    public readonly float DelayToTransform = 1f;
}
