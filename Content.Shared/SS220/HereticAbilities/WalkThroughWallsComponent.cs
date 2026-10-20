using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.HereticAbilities;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class WalkThroughWallsComponent : Component
{
    [DataField]
    public bool IsInfinity;

    [DataField]
    public float? Duration;

    [DataField]
    public int BulletImpassable = (int)CollisionGroup.BulletImpassable;

    [DataField]
    public int WallsLayer = (int)CollisionGroup.None;

    public bool IsWalked;

    public int? PreviousGroupLayer;
    public int? PreviousGroupMask;

    public string Fixture = "fix1";
}
