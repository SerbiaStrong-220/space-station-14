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

    /// <summary>
    /// Tick in which a projectile already rolled its own trauma for a hit.
    /// The damage of that hit is applied right after and must not roll a second trauma.
    /// </summary>
    [ViewVariables]
    public GameTick ProjectileTraumaTick = GameTick.Zero;
}

[Serializable, NetSerializable]
public sealed partial class PathologyInstanceData(TimeSpan startTime, IPathologyContext? context)
{
    [ViewVariables]
    public TimeSpan StartTime = startTime;

    [ViewVariables]
    public int Level = 0;

    [ViewVariables]
    public int StackCount = SharedPathologySystem.OneStack;

    [ViewVariables]
    public List<IPathologyContext?> PathologyContexts = new() { context };
}
