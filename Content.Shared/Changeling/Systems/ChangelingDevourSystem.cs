// SS220 Changeling
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Armor;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.SS220.Grab;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingDevourSystem : EntitySystem // SS220 Changeling - partial
{
    private static readonly ProtoId<DamageTypePrototype> HeatDamage = "Heat";
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationDamage = "Asphyxiation";

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentitySystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingDevourComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourWindupDoAfterEvent>(OnDevourWindup);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourConsumeDoAfterEvent>(OnDevourConsume);
        SubscribeLocalEvent<ChangelingDevourComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<ChangelingDevourComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.DevourWindupTime < TimeSpan.Zero)
            ent.Comp.DevourWindupTime = TimeSpan.Zero;
        if (ent.Comp.DevourConsumeTime < TimeSpan.Zero)
            ent.Comp.DevourConsumeTime = TimeSpan.Zero;
        if (!float.IsFinite(ent.Comp.DevourPreventionPercentageThreshold))
            ent.Comp.DevourPreventionPercentageThreshold = 0.1f;
        ent.Comp.DevourPreventionPercentageThreshold = Math.Clamp(
            ent.Comp.DevourPreventionPercentageThreshold,
            0f,
            1f);

        _actionsSystem.AddAction(ent, ref ent.Comp.ChangelingDevourActionEntity, ent.Comp.ChangelingDevourAction);
    }

    private void OnShutdown(Entity<ChangelingDevourComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer)
            ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);

        if (_net.IsServer &&
            !TerminatingOrDeleted(ent.Owner) &&
            ent.Comp.ChangelingDevourActionEntity != null)
        {
            _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ChangelingDevourActionEntity);
            QueueDel(ent.Comp.ChangelingDevourActionEntity.Value);
        }
    }

    // The action was used.
    // Start the first doafter for the windup.
    private void OnDevourAction(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourActionEvent args)
    {
        if (args.Handled
            || args.Performer != ent.Owner
            || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target)
            || !HasComp<ChangelingIdentityComponent>(ent))
            return;

        args.Handled = true;
        var target = args.Target;

        if (!CanDevour(ent.AsNullable(), target))
            return;

        if (!_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
                ent,
                ent.Comp.DevourWindupTime,
                new ChangelingDevourWindupDoAfterEvent(),
                ent,
                target: target,
                used: ent)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = false,
            DuplicateCondition = DuplicateConditions.SameEvent,
        }))
        {
            return;
        }

        if (_net.IsServer)
        {
            ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);
            ent.Comp.CurrentDevourSound = _audio.PlayPvs(ent.Comp.DevourWindupNoise, ent)?.Entity;
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ent:player} started changeling devour windup against {target:player}");

        var selfMessage = Loc.GetString("changeling-devour-begin-windup-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-begin-windup-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            args.Performer,
            args.Performer,
            PopupType.MediumCaution);
    }

    // First doafter finished.
    // Start the second doafter for the actual consumption and deal a small amount of damage.
    private void OnDevourWindup(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourWindupDoAfterEvent args)
    {
        args.Handled = true;
        ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);

        if (args.Cancelled)
            return;

        if (args.Target is not { } target || !CanDevour(ent.AsNullable(), target, showPopup: false))
            return;

        if (!_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
                ent,
                ent.Comp.DevourConsumeTime,
                new ChangelingDevourConsumeDoAfterEvent(),
                ent,
                target: target,
                used: ent)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = false,
            DuplicateCondition = DuplicateConditions.SameEvent,
        }))
        {
            return;
        }

        _damageable.ChangeDamage(target, ent.Comp.WindupDamage, true, true, ent.Owner);

        var selfMessage = Loc.GetString("changeling-devour-begin-consume-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-begin-consume-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            ent.Owner,
            ent.Owner,
            PopupType.LargeCaution);

        if (_net.IsServer)
            ent.Comp.CurrentDevourSound = _audio.PlayPvs(ent.Comp.ConsumeNoise, ent)?.Entity;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} began to devour {ToPrettyString(target):player}'s identity");
    }

    // Second doafter finished.
    // Save the identity and deal more damage.
    private void OnDevourConsume(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourConsumeDoAfterEvent args)
    {
        args.Handled = true;
        ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);

        if (args.Cancelled)
            return;

        if (args.Target is not { } target || !CanDevour(ent.AsNullable(), target, showPopup: false))
            return;

        // Identity snapshots, damage, rewards, and the duplicate-devour marker are one authoritative server commit.
        // The client cannot create paused-map identity clones and must not predict a storage failure or partial success.
        if (_net.IsClient)
            return;

        if (!TryComp<ChangelingIdentityComponent>(ent.Owner, out var identityStorage))
            return;

        var genome = _changelingIdentitySystem.GetGenomeId(target);
        if (genome == null)
            return;

        var compromisedGenome = HasExcessiveBurnOrAsphyxiation(target);
        if (!_changelingIdentitySystem.HasStoredGenome((ent.Owner, identityStorage), genome))
        {
            if (!_changelingIdentitySystem.TryStoreIdentity(
                    (ent.Owner, identityStorage),
                    target,
                    countForObjective: !compromisedGenome,
                    out _))
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("changeling-devour-attempt-failed-storage"),
                    ent.Owner,
                    ent.Owner,
                    PopupType.Medium);
                return;
            }
        }
        else if (!compromisedGenome)
        {
            _changelingIdentitySystem.RecordAbsorbedGenome((ent.Owner, identityStorage), genome, target);
        }

        // Identity storage is the only fallible part of committing a devour. Complete it before irreversible
        // victim damage, clothing destruction, success feedback, rewards, and the duplicate-devour marker.
        _damageable.ChangeDamage(target, ent.Comp.DevourDamage, true, true, ent.Owner);

        var selfMessage = Loc.GetString("changeling-devour-consume-complete-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-consume-complete-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupEntity(selfMessage, ent.Owner, ent.Owner, PopupType.LargeCaution);
        _popupSystem.PopupEntity(
            othersMessage,
            ent.Owner,
            Filter.PvsExcept(ent.Owner),
            true,
            PopupType.LargeCaution);

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} successfully devoured {ToPrettyString(target):player}'s identity");

        if (_inventorySystem.TryGetSlotEntity(target, "jumpsuit", out var item)
            && TryComp<ButcherableComponent>(item, out var butcherable))
            RipClothing(target, (item.Value, butcherable));

        var chemicalGain = FixedPoint2.New(10);
        if (TryComp<ChangelingResourceComponent>(target, out var victimResources))
            chemicalGain = victimResources.Chemicals;

        var addChemicals = new ChangelingAddChemicalsEvent(chemicalGain);
        RaiseLocalEvent(ent.Owner, ref addChemicals);

        var resetEvolution = new ChangelingResetEvolutionEvent();
        RaiseLocalEvent(ent.Owner, ref resetEvolution);

        // We add a reference to ourselves to prevent repeated identity gain.
        var targetDevoured = EnsureComp<ChangelingDevouredComponent>(target);
        targetDevoured.DevouredBy.Add(ent.Owner);
    }

    /// <summary>
    /// Has the given victim been devoured by the given changeling before?
    /// </summary>
    public bool HasDevoured(Entity<ChangelingIdentityComponent?> changeling, EntityUid devoured)
    {
        if (!Resolve(changeling, ref changeling.Comp, false))
            return false;

        return TryComp<ChangelingDevouredComponent>(devoured, out var consumed)
            && consumed.DevouredBy.Contains(changeling.Owner);
    }

    /// <summary>
    /// Can the given changeling devour the given victim?
    /// </summary>
    public bool CanDevour(Entity<ChangelingDevourComponent?> changeling, EntityUid victim, bool showPopup = true)
    {
        if (!Resolve(changeling, ref changeling.Comp))
            return false;

        if (changeling.Owner == victim ||
            TerminatingOrDeleted(changeling.Owner) ||
            TerminatingOrDeleted(victim))
            return false;

        if (!HasComp<HumanoidProfileComponent>(victim))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-cannot-devour"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (HasDevoured(changeling.Owner, victim))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-already-devoured"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (!_mobState.IsDead(victim)
            && (!TryComp<GrabbableComponent>(victim, out var grabbable)
                || grabbable.GrabbedBy != changeling.Owner
                || grabbable.GrabStage < GrabStage.NeckGrab))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-neck-grab"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (HasComp<RottingComponent>(victim))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-rotting"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (IsTargetProtected(victim, changeling!))
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-protected"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        if (TryComp<ChangelingIdentityComponent>(changeling.Owner, out var identities)
            && _changelingIdentitySystem.GetGenomeId(victim) is { } genome
            && !_changelingIdentitySystem.HasStoredGenome((changeling.Owner, identities), genome)
            && identities.ConsumedIdentities.Count >= identities.MaxStoredIdentities)
        {
            if (showPopup)
                _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-storage-full"), changeling.Owner, changeling.Owner, PopupType.Medium);
            return false;
        }

        return true;
    }

    private bool HasExcessiveBurnOrAsphyxiation(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return false;

        var damage = _damageable.GetPositiveDamage((target, damageable));
        damage.DamageDict.TryGetValue(HeatDamage, out var heat);
        damage.DamageDict.TryGetValue(AsphyxiationDamage, out var asphyxiation);
        return heat + asphyxiation >= FixedPoint2.New(50);
    }

    /// <summary>
    /// Checks if the target's outerclothing is beyond a DamageCoefficientThreshold to protect them from being devoured.
    /// </summary>
    /// <param name="target">The Targeted entity</param>
    /// <param name="ent">Changelings Devour Component</param>
    /// <returns>Is the target Protected from the attack</returns>
    private bool IsTargetProtected(EntityUid target, Entity<ChangelingDevourComponent> ent)
    {
        var ev = new CoefficientQueryEvent(SlotFlags.OUTERCLOTHING);

        RaiseLocalEvent(target, ev);

        foreach (var compProtectiveDamageType in ent.Comp.ProtectiveDamageTypes)
        {
            if (!ev.DamageModifiers.Coefficients.TryGetValue(compProtectiveDamageType, out var coefficient))
                continue;
            if (coefficient < 1f - ent.Comp.DevourPreventionPercentageThreshold)
                return true;
        }

        return false;
    }

    // TODO: This should just be an API method in the butcher system
    private void RipClothing(EntityUid victim, Entity<ButcherableComponent> item)
    {
        var spawnEntities = EntitySpawnCollection.GetSpawns(item.Comp.SpawnedEntities, _robustRandom);

        foreach (var proto in spawnEntities)
        {
            // TODO: once predictedRandom is in, make this a Coordinate offset of 0.25f from the victims position
            PredictedSpawnNextToOrDrop(proto, victim);
        }

        PredictedQueueDel(item.Owner);
    }
}
