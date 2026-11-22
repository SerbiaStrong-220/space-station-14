// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Zombies;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.Body;
using Content.Shared.SS220.Pathology;
using Content.Shared.Tag;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Body;

/// <summary>
/// Marks the organs taken from a zombie, an infected or a rotten body. Put into another body they poison it while any of them stays,
/// an organ of a zombie or an infected body also infects it the way a zombie bite does.
/// </summary>
public sealed partial class TaintedOrganSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ZombieSystem _zombie = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<PathologyPrototype> TaintedFleshPathology = "TaintedFlesh";
    private static readonly ProtoId<TagPrototype> ZombifyableTag = "ZombifyableByMelee";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TaintableOrganComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<TaintableOrganComponent, OrganGotInsertedEvent>(OnInserted);
    }

    private void OnRemoved(Entity<TaintableOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || TerminatingOrDeleted(ent) || HasComp<ProsthesisComponent>(ent))
            return;

        ent.Comp.Zombified |= HasComp<ZombieComponent>(args.Target) || HasComp<PendingZombieComponent>(args.Target);
        ent.Comp.Rotten |= ent.Comp.PoisonsWhenRotten && _rotting.IsRotten(args.Target);

        // one poisoning for all the tainted organs, it ends with the last of them
        if (IsTainted(ent.Comp) && !HasTaintedOrgan(args.Target))
            _pathology.TryRemovePathology(args.Target, TaintedFleshPathology, checkStacks: false);
    }

    private void OnInserted(Entity<TaintableOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!IsTainted(ent.Comp))
            return;

        _pathology.TryAddPathology(args.Target, TaintedFleshPathology);

        if (ent.Comp.Zombified)
            Infect(args.Target);
    }

    /// <summary>
    /// The same as a zombie bite: a living body gets the infection, a crit or dead one turns at once.
    /// </summary>
    private void Infect(EntityUid body)
    {
        if (HasComp<ZombieComponent>(body)
            || HasComp<IncurableZombieComponent>(body)
            || HasComp<ZombieImmuneComponent>(body)
            || !_tag.HasTag(body, ZombifyableTag))
            return;

        if (_mobState.IsAlive(body))
        {
            EnsureComp<PendingZombieComponent>(body);
            EnsureComp<ZombifyOnDeathComponent>(body);
        }
        else
        {
            _zombie.ZombifyEntity(body);
        }
    }

    private bool HasTaintedOrgan(EntityUid body)
    {
        _body.TryGetOrgansWithComponent<TaintableOrganComponent>(body, out var organs);
        foreach (var organ in organs)
        {
            if (IsTainted(organ.Comp))
                return true;
        }

        return false;
    }

    private static bool IsTainted(TaintableOrganComponent organ)
    {
        return organ.Zombified || organ.Rotten;
    }
}
