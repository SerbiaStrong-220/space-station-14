// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.SS220.SpiderQueen;

public sealed partial class SpiderTargetSpawnEvent : WorldTargetActionEvent
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
    /// The cost of blood points to use this action
    /// </summary>
    [DataField]
    public FixedPoint2 Cost = FixedPoint2.Zero;

    /// <summary>
    /// The time it takes before spawn entities
    /// </summary>
    [DataField]
    public TimeSpan DoAfter = TimeSpan.Zero;
}

public sealed partial class SpiderCocooningActionEvent : EntityTargetActionEvent
{
    /// <summary>
    /// The time it takes to create a cocoon on the target
    /// </summary>
    [DataField]
    public TimeSpan CocooningTime = TimeSpan.Zero;
}

public sealed partial class SpiderNearbySpawnEvent : InstantActionEvent
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
    /// The cost of blood points to use this action
    /// </summary>
    [DataField]
    public FixedPoint2 Cost = FixedPoint2.Zero;

    /// <summary>
    /// The time it takes before spawn entities
    /// </summary>
    [DataField]
    public TimeSpan DoAfter = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed partial class AfterCocooningEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class SpiderSpawnDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// The coordinates of the location that the user targeted.
    /// </summary>
    public NetCoordinates TargetCoordinates;

    /// <summary>
    /// List of prototypes to spawn
    /// </summary>
    public List<EntitySpawnEntry> Prototypes = new();

    /// <summary>
    /// The offset the prototypes will spawn in on relative to the one prior.
    /// Set to 0,0 to have them spawn on the same tile.
    /// </summary>
    public Vector2 Offset;

    /// <summary>
    /// The cost of blood points to use this action
    /// </summary>
    public FixedPoint2 Cost = FixedPoint2.Zero;
}

[Serializable, NetSerializable]
public sealed partial class CocoonExtractBloodPointsEvent : SimpleDoAfterEvent
{
}
