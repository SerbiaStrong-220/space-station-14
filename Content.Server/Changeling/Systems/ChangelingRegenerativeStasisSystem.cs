// SS220 Changeling
using System.Collections.Generic;
using System.Linq;
using Content.Server.Buckle.Systems;
using Content.Server.Ensnaring;
using Content.Server.SS220.Grab;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Ensnaring.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Systems;

/// <summary>
/// Handles the changeling's fake death and delayed full-body regeneration.
/// </summary>
public sealed class ChangelingRegenerativeStasisSystem : EntitySystem
{
    private static readonly ProtoId<OrganCategoryPrototype> HeadCategory = "Head";

    [Dependency] private readonly ChangelingResourceSystem _resources = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BuckleSystem _buckle = default!;
    [Dependency] private readonly GrabSystem _grab = default!;
    [Dependency] private readonly EnsnareableSystem _ensnareable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingResourceComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingRegenerativeStasisActionEvent>(OnEnterStasis);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingRegenerateActionEvent>(OnRegenerate);
        SubscribeLocalEvent<ChangelingResourceComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        SubscribeLocalEvent<ChangelingResourceComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnStartup(Entity<ChangelingResourceComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.RegenerativeStasisActionEntity, ent.Comp.RegenerativeStasisAction);
        _actions.AddAction(ent, ref ent.Comp.RegenerateActionEntity, ent.Comp.RegenerateAction);

        RefreshActions(ent);
    }

    private void OnEnterStasis(Entity<ChangelingResourceComponent> ent,
        ref ChangelingRegenerativeStasisActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            ent.Comp.InRegenerativeStasis ||
            !CanRegenerate(ent))
            return;

        if (ent.Comp.RegenerativeStasisChemicalCost < FixedPoint2.Zero)
        {
            Log.Error($"Changeling {ToPrettyString(ent.Owner)} has a negative regenerative stasis cost.");
            return;
        }

        if (!_resources.TrySpendChemicals(ent.AsNullable(), ent.Comp.RegenerativeStasisChemicalCost))
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), ent.Owner, ent.Owner);
            return;
        }

        args.Handled = true;
        ent.Comp.InRegenerativeStasis = true;
        var duration = ent.Comp.RegenerativeStasisDuration < TimeSpan.Zero
            ? TimeSpan.Zero
            : ent.Comp.RegenerativeStasisDuration;
        ent.Comp.CanRegenerateAt = _timing.CurTime + duration;
        Dirty(ent);

        _mobState.ChangeMobState(ent.Owner, MobState.Dead);
        RefreshActions(ent);
        _popup.PopupEntity(Loc.GetString("changeling-regenerative-stasis-entered"), ent.Owner, ent.Owner);
    }

    private void OnRegenerate(Entity<ChangelingResourceComponent> ent, ref ChangelingRegenerateActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            !ent.Comp.InRegenerativeStasis ||
            ent.Comp.CanRegenerateAt is not { } readyAt ||
            _timing.CurTime < readyAt ||
            !CanRegenerate(ent))
            return;

        if (!TryRestoreMissingInitialOrgans(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("changeling-regenerative-stasis-restore-failed"), ent.Owner, ent.Owner);
            return;
        }

        args.Handled = true;
        ent.Comp.InRegenerativeStasis = false;
        ent.Comp.CanRegenerateAt = null;
        Dirty(ent);

        // Rejuvenate is the canonical full heal: damage, blood loss, diseases, stamina,
        // and removable status effects all clean themselves up through this event.
        RaiseLocalEvent(ent.Owner, new RejuvenateEvent());
        _mobState.ChangeMobState(ent.Owner, MobState.Alive);

        ReleaseRestraints(ent.Owner);
        RefreshActions(ent);
        _popup.PopupEntity(Loc.GetString("changeling-regenerated"), ent.Owner, ent.Owner);
    }

    private void OnOrganRemoved(Entity<ChangelingResourceComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (!TryComp<OrganComponent>(args.Organ, out var organ) || organ.Category != HeadCategory)
            return;

        PermanentlyBlockRegeneration(ent);
    }

    private void OnBeingGibbed(Entity<ChangelingResourceComponent> ent, ref BeingGibbedEvent args)
    {
        PermanentlyBlockRegeneration(ent);
    }

    private bool CanRegenerate(Entity<ChangelingResourceComponent> ent)
    {
        if (ent.Comp.RegenerationPermanentlyBlocked ||
            HasComp<BorgChassisComponent>(ent) ||
            !HasComp<MobStateComponent>(ent) ||
            !HasComp<InitialBodyComponent>(ent) ||
            !TryComp<BodyComponent>(ent, out var body) ||
            body.Organs == null)
            return false;

        foreach (var organUid in body.Organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organUid, out var organ) && organ.Category == HeadCategory)
                return true;
        }

        return false;
    }

    private bool TryRestoreMissingInitialOrgans(EntityUid uid)
    {
        if (!TryComp<BodyComponent>(uid, out var body) ||
            body.Organs == null ||
            !TryComp<InitialBodyComponent>(uid, out var initialBody))
        {
            Log.Error($"Unable to restore organs for {ToPrettyString(uid)}: its initial body data is missing.");
            return false;
        }

        var presentCategories = new HashSet<ProtoId<OrganCategoryPrototype>>();
        foreach (var organUid in body.Organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organUid, out var organ) && organ.Category is { } category)
                presentCategories.Add(category);
        }

        foreach (var (category, prototype) in initialBody.Organs)
        {
            if (category == HeadCategory || presentCategories.Contains(category))
                continue;

            var organUid = EntityManager.SpawnInContainerOrDrop(
                prototype,
                uid,
                BodyComponent.ContainerID,
                out var inserted,
                Transform(uid));
            if (!inserted ||
                !TryComp<OrganComponent>(organUid, out var organ) ||
                organ.Category != category)
            {
                QueueDel(organUid);
                Log.Error($"Unable to restore {category} organ for {ToPrettyString(uid)} from prototype {prototype}.");
                return false;
            }

            presentCategories.Add(category);
        }

        return true;
    }

    private void PermanentlyBlockRegeneration(Entity<ChangelingResourceComponent> ent)
    {
        if (ent.Comp.RegenerationPermanentlyBlocked)
            return;

        ent.Comp.RegenerationPermanentlyBlocked = true;
        Dirty(ent);
        RefreshActions(ent);
    }

    private void RefreshActions(Entity<ChangelingResourceComponent> ent)
    {
        var canRegenerate = CanRegenerate(ent);
        _actions.SetEnabled(ent.Comp.RegenerativeStasisActionEntity,
            canRegenerate && !ent.Comp.InRegenerativeStasis);
        _actions.SetEnabled(ent.Comp.RegenerateActionEntity,
            canRegenerate && ent.Comp.InRegenerativeStasis);

        if (!ent.Comp.InRegenerativeStasis || ent.Comp.CanRegenerateAt is not { } readyAt)
        {
            _actions.RemoveCooldown(ent.Comp.RegenerateActionEntity);
            return;
        }

        if (_timing.CurTime < readyAt)
            _actions.SetCooldown(ent.Comp.RegenerateActionEntity, _timing.CurTime, readyAt);
        else
            _actions.RemoveCooldown(ent.Comp.RegenerateActionEntity);
    }

    private void ReleaseRestraints(EntityUid uid)
    {
        // Rejuvenate removes handcuffs. Explicitly release the remaining restraint systems
        // that are not Rejuvenate subscribers.
        if (TryComp<BuckleComponent>(uid, out var buckle))
            _buckle.TryUnbuckle(uid, uid, buckle, false);

        _grab.BreakGrab(uid);

        if (!TryComp<EnsnareableComponent>(uid, out var ensnareable))
            return;

        foreach (var ensnare in ensnareable.Container.ContainedEntities.ToArray())
        {
            if (TryComp<EnsnaringComponent>(ensnare, out var ensnaring))
                _ensnareable.ForceFree(ensnare, ensnaring);
        }
    }
}
