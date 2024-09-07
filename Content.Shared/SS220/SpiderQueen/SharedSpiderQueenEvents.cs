// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using System.Numerics;

namespace Content.Shared.SS220.SpiderQueen;

public sealed partial class SpiderWorldSpawnEvent : WorldTargetActionEvent
{
    /// <summary>
    /// The list of prototypes will spawn
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> Prototypes = new();

    /// <summary>
    /// The offset the prototypes will spawn in on relative to the one prior.
    /// Set to 0,0 to have them spawn on the same tile.
    /// </summary>
    [DataField]
    public Vector2 Offset;

    /// <summary>
    /// The cost of mana to use this action
    /// </summary>
    [DataField]
    public FixedPoint2 Cost = FixedPoint2.Zero;
}
