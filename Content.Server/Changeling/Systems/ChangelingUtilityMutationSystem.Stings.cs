// SS220 Changeling
using Content.Server.Changeling.Components;
using Content.Shared.Body.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingUtilityMutationSystem
{
    // Reused synchronous payload for the Update-driven cryogenic sting. The value is reset before every call
    // because the damage pipeline may apply global modifiers to the supplied specifier in place.
    private readonly DamageSpecifier _cryogenicStingDamage = new()
    {
        DamageDict = { { ColdDamage, FixedPoint2.Zero } },
    };

    private void InitializeChemicalStings()
    {
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingMuteStingActionEvent>(OnMuteSting);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingBlindStingActionEvent>(OnBlindSting);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingCryogenicStingActionEvent>(OnCryogenicSting);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingLethargicStingActionEvent>(OnLethargicSting);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingFakeArmbladeStingActionEvent>(OnFakeArmbladeSting);
        SubscribeLocalEvent<ChangelingCryogenicStingComponent, RefreshMovementSpeedModifiersEvent>(OnCryogenicMovement);
        SubscribeLocalEvent<ChangelingCryogenicStingComponent, ComponentShutdown>(OnCryogenicShutdown);
        SubscribeLocalEvent<ChangelingBlindStingComponent, CanSeeAttemptEvent>(OnBlindStingCanSee);
        SubscribeLocalEvent<ChangelingBlindStingComponent, ComponentShutdown>(OnBlindStingShutdown);
        SubscribeLocalEvent<ChangelingBlindStingComponent, RejuvenateEvent>(OnBlindStingRejuvenate);
    }

    private void UpdateBlindStings(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ChangelingBlindStingComponent>();
        while (query.MoveNext(out var uid, out var blind))
        {
            if (now >= blind.EndTime)
                RemComp<ChangelingBlindStingComponent>(uid);
        }
    }

    private void UpdateCryogenicStings(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ChangelingCryogenicStingComponent>();
        while (query.MoveNext(out var uid, out var cryo))
        {
            if (now >= cryo.EndTime)
            {
                RemComp<ChangelingCryogenicStingComponent>(uid);
                continue;
            }

            if (now < cryo.NextTick)
                continue;

            cryo.NextTick += CryogenicStingTickInterval;
            _cryogenicStingDamage.DamageDict.Clear();
            _cryogenicStingDamage.ArmourPiercing = FixedPoint2.Zero;
            _cryogenicStingDamage.DamageDict.Add(ColdDamage, FixedPoint2.New(2 * cryo.DamageMultiplier));
            _damage.ChangeDamage(uid, _cryogenicStingDamage, true, false);
        }
    }

    private bool BeginSting(EntityUid owner, EntityUid target, int cost)
    {
        if (TerminatingOrDeleted(owner) ||
            TerminatingOrDeleted(target) ||
            owner == target ||
            !_interaction.InRangeUnobstructed(owner, target) ||
            !TryComp<BloodstreamComponent>(target, out _))
        {
            return false;
        }

        if (!Spend(owner, cost))
            return false;

        if (HasComp<ChangelingIdentityComponent>(target) || HasComp<ChangelingLesserFormComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-sting-target-is-changeling"), owner, owner);
            return false;
        }

        return true;
    }

    private void Inject(EntityUid target, ProtoId<ReagentPrototype> reagent, float amount)
    {
        var solution = new Solution();
        solution.AddReagent(reagent, FixedPoint2.New(amount));
        _bloodstream.TryAddToBloodstream(target, solution);
    }

    private void OnMuteSting(Entity<ChangelingResourceComponent> ent, ref ChangelingMuteStingActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !BeginSting(ent.Owner, args.Target, 20))
            return;

        args.Handled = true;
        Inject(args.Target, MuteToxin, 5f);
    }

    private void OnBlindSting(Entity<ChangelingResourceComponent> ent, ref ChangelingBlindStingActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !BeginSting(ent.Owner, args.Target, 25))
            return;

        args.Handled = true;
        var blind = EnsureComp<ChangelingBlindStingComponent>(args.Target);
        var endTime = _timing.CurTime + TimeSpan.FromSeconds(15);
        if (blind.EndTime < endTime)
            blind.EndTime = endTime;
        _blindable.UpdateIsBlind(args.Target);
        _blindable.AdjustEyeDamage(args.Target, 3);
    }

    private void OnBlindStingCanSee(Entity<ChangelingBlindStingComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.LifeStage < ComponentLifeStage.Stopping && _timing.CurTime < ent.Comp.EndTime)
            args.Cancel();
    }

    private void OnBlindStingShutdown(Entity<ChangelingBlindStingComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnBlindStingRejuvenate(Entity<ChangelingBlindStingComponent> ent, ref RejuvenateEvent args)
    {
        RemComp<ChangelingBlindStingComponent>(ent.Owner);
    }

    private void OnCryogenicSting(
        Entity<ChangelingResourceComponent> ent,
        ref ChangelingCryogenicStingActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !BeginSting(ent.Owner, args.Target, 15))
            return;

        args.Handled = true;
        Inject(args.Target, Nitrogen, 10f);
        var cryo = EnsureComp<ChangelingCryogenicStingComponent>(args.Target);
        cryo.EndTime = _timing.CurTime + TimeSpan.FromSeconds(20);
        cryo.NextTick = _timing.CurTime;
        cryo.DamageMultiplier = _inventory.TryGetSlotEntity(args.Target, "outerClothing", out _) ? 1.5f : 1f;
        _movement.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnCryogenicMovement(
        Entity<ChangelingCryogenicStingComponent> ent,
        ref RefreshMovementSpeedModifiersEvent args)
    {
        // ComponentShutdown refreshes movement while the component can still receive directed events.
        // The lifecycle guard prevents that refresh from reapplying the effect being removed.
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping || _timing.CurTime >= ent.Comp.EndTime)
            return;

        args.ModifySpeed(0.6f);
    }

    private void OnCryogenicShutdown(Entity<ChangelingCryogenicStingComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnLethargicSting(
        Entity<ChangelingResourceComponent> ent,
        ref ChangelingLethargicStingActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !BeginSting(ent.Owner, args.Target, 50))
            return;

        args.Handled = true;
        Inject(args.Target, Nocturine, 12f);
        Inject(args.Target, MuteToxin, 5f);
    }

    private void OnFakeArmbladeSting(
        Entity<ChangelingResourceComponent> ent,
        ref ChangelingFakeArmbladeStingActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !BeginSting(ent.Owner, args.Target, 30))
            return;

        var blade = Spawn(FakeArmBlade, Transform(args.Target).Coordinates);
        if (!_hands.TryForcePickupAnyHand(args.Target, blade))
        {
            QueueDel(blade);
            _resources.AddChemicals(ent.Owner, FixedPoint2.New(30));
            _popup.PopupEntity(Loc.GetString("changeling-fake-armblade-failed"), ent.Owner, ent.Owner);
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("changeling-fake-armblade-used"), ent.Owner, ent.Owner);
    }
}
