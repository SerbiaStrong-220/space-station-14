using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Grab;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabberComponent : Component
{
    [DataField]
    public Vector2 GrabOffset = new Vector2(0, -0.5f);

    [DataField]
    public EntityUid? Grabbing;
}
