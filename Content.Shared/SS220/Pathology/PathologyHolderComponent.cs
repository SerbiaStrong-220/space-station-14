// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Traits;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PathologyHolderComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public Dictionary<ProtoId<PathologyPrototype>, PathologyInstanceData> ActivePathologies = new();

    [ViewVariables]
    [AutoNetworkedField]
    public HashSet<ProtoId<TraitPrototype>> AppliedTraits = new();

    [ViewVariables]
    public TimeSpan NextUpdate;

    // Traumas from the damage of one hit, rolled on the server. Null - the trauma doesn't happen from damage.
    // Bone traumas are exclusive: only the heaviest one whose threshold is reached is rolled.

    /// <summary>
    /// By blunt damage.
    /// </summary>
    [DataField]
    public TraumaRoll? DislocationRoll;

    /// <summary>
    /// By blunt damage.
    /// </summary>
    [DataField]
    public TraumaRoll? HairlineFractureRoll;

    /// <summary>
    /// By blunt damage.
    /// </summary>
    [DataField]
    public TraumaRoll? CompoundFractureRoll;

    /// <summary>
    /// By slash and piercing damage.
    /// </summary>
    [DataField]
    public TraumaRoll? ArterialPunctureRoll;

    /// <summary>
    /// By heat, caustic and shock damage.
    /// </summary>
    [DataField]
    public TraumaRoll? BurnNecrosisRoll;

    /// <summary>
    /// Tick in which a projectile already rolled its own trauma for a hit.
    /// The damage of that hit is applied right after and must not roll a second trauma.
    /// </summary>
    [ViewVariables]
    public GameTick ProjectileTraumaTick = GameTick.Zero;

    /// <summary>
    /// True while damage that is not a hit (e.g. of a surgery step) is being applied, it must not roll traumas or tear body parts off.
    /// Set only by <see cref="SharedPathologySystem.TryChangeDamageWithoutTraumas"/>.
    /// </summary>
    [ViewVariables]
    public bool DamageWithoutTraumas;
}

[Serializable, NetSerializable]
public sealed partial class PathologyInstanceData(TimeSpan startTime, IPathologyContext? context)
{
    [ViewVariables]
    public TimeSpan StartTime = startTime;

    /// <summary>
    /// When the current level was reached, timed progress counts from it.
    /// </summary>
    [ViewVariables]
    public TimeSpan LevelStartTime = startTime;

    [ViewVariables]
    public int Level = 0;

    [ViewVariables]
    public int StackCount = SharedPathologySystem.OneStack;

    [ViewVariables]
    public List<IPathologyContext?> PathologyContexts = new() { context };
}
