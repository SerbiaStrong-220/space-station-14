// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Movement.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Body;

/// <summary>
/// Moves the attached organs together with their limb, makes a body that lost all supporting limbs crawl,
/// hides the damage overlays of missing limbs and keeps clothes off the missing organs they are worn on.
/// </summary>
public sealed partial class LimbSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LimbComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<LimbComponent, OrganGotInsertedEvent>(OnInserted);

        InitializeWornSlots();
    }

    private void OnInit(Entity<LimbComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent, LimbComponent.ContainerId);
    }

    // organ moves come to clients with the container state, doing them there too would desync
    private void OnRemoved(Entity<LimbComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_net.IsClient || TerminatingOrDeleted(args.Target) || TerminatingOrDeleted(ent))
            return;

        foreach (var category in ent.Comp.AttachedOrgans)
        {
            if (_body.TryGetOrganByCategory(args.Target, category, out var organ))
                _container.Insert(organ.Value.Owner, ent.Comp.Container);
        }

        DebugTools.Assert(ent.Comp.Container.ContainedEntities.Count <= ent.Comp.AttachedOrgans.Count,
            $"Limb {ToPrettyString(ent)} carries more organs than it has attached categories");

        if (ent.Comp.Supporting && !HasSupportingLimb(args.Target) && !HasComp<WormComponent>(args.Target))
        {
            AddComp<WormComponent>(args.Target);
            AddComp<LimbWormComponent>(args.Target);
        }

        SetDamageOverlayDisabled(ent, args.Target, true);
    }

    private void OnInserted(Entity<LimbComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_net.IsClient)
            return;

        ReturnAttachedOrgans(ent.AsNullable(), args.Target);

        if (ent.Comp.Supporting && RemComp<LimbWormComponent>(args.Target))
            RemComp<WormComponent>(args.Target);

        SetDamageOverlayDisabled(ent, args.Target, false);
    }

    /// <summary>
    /// The damage visualizer hides the overlays of a target layer whose appearance data under the layer key is true.
    /// </summary>
    private void SetDamageOverlayDisabled(EntityUid limb, EntityUid body, bool disabled)
    {
        if (TryComp<VisualOrganComponent>(limb, out var visual))
            _appearance.SetData(body, visual.Layer, disabled);
    }

    /// <summary>
    /// Puts the organs carried by the limb into the body, the ones the body already has are dropped next to it.
    /// </summary>
    public void ReturnAttachedOrgans(Entity<LimbComponent?> limb, EntityUid body)
    {
        if (!Resolve(limb, ref limb.Comp, logMissing: false))
            return;

        var attached = limb.Comp.Container.ContainedEntities;
        for (var i = attached.Count - 1; i >= 0; i--)
        {
            var organ = attached[i];
            if (!_body.TryInsertOrgan(body, organ))
                _transform.DropNextTo(organ, body);
        }
    }

    private bool HasSupportingLimb(EntityUid body)
    {
        _body.TryGetOrgansWithComponent<LimbComponent>(body, out var limbs);
        foreach (var limb in limbs)
        {
            if (limb.Comp.Supporting)
                return true;
        }

        return false;
    }
}
