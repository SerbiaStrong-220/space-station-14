// SS220 Changeling
using Content.Server.Light.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Kitchen.Components;
using Content.Shared.Light.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Store.Components;
using Content.Shared.Stunnable;

namespace Content.Server.Changeling.Mutations;

public sealed partial class ChangelingMutationSystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private void OnArmBlade(Entity<ChangelingResourceComponent> ent, ref ChangelingArmBladeActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var state = EnsureComp<ChangelingMutationStateComponent>(ent);
        if (state.ArmBlade is { } activeBlade && !TerminatingOrDeleted(activeBlade))
        {
            args.Handled = true;
            // DeactivateArmBlade updates the action state explicitly because it is also used by forced cleanup.
            // Do not ask SharedActionsSystem to invert that state again after this handler returns.
            args.Toggle = false;
            DeactivateArmBlade((ent.Owner, state));
            PlayMutationRetractSound(ent.Owner);
            PopupMutation(ent.Owner, "changeling-arm-blade-retracted");
            return;
        }

        if (!_hands.TryGetEmptyHand(ent.Owner, out var hand) || !TrySpend(ent, args.ChemicalCost))
            return;

        var blade = Spawn(ArmBladePrototype, Transform(ent).Coordinates);
        if (!_hands.TryPickup(ent.Owner, blade, hand, checkActionBlocker: false))
        {
            QueueDel(blade);
            _resources.AddChemicals(ent.Owner, FixedPoint2.New(args.ChemicalCost));
            return;
        }

        args.Handled = true;
        args.Toggle = true;
        state.ArmBlade = blade;
        state.ArmBladeAction = args.Action;
        state.NextArmBladeDrain = _timing.CurTime + ArmBladeDrainInterval;
        Dirty(ent.Owner, state);
        PlayMutationFormSound(ent.Owner);
        PopupMutation(ent.Owner, "changeling-arm-blade-formed");
    }

    private void DeactivateArmBlade(Entity<ChangelingMutationStateComponent> ent)
    {
        if (ent.Comp.ArmBlade is { } blade && !TerminatingOrDeleted(blade))
            QueueDel(blade);

        if (ent.Comp.ArmBladeAction is { } action)
            _actions.SetToggled(action, false);

        ent.Comp.ArmBlade = null;
        ent.Comp.ArmBladeAction = null;
        ent.Comp.NextArmBladeDrain = TimeSpan.Zero;
        Dirty(ent);
    }

    private void OnBoneShard(Entity<ChangelingResourceComponent> ent, ref ChangelingBoneShardActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            !_hands.TryGetEmptyHand(ent.Owner, out var hand) ||
            !TrySpend(ent, args.ChemicalCost))
            return;

        var shard = Spawn(BoneShardPrototype, Transform(ent).Coordinates);
        if (!_hands.TryPickup(ent.Owner, shard, hand, checkActionBlocker: false))
        {
            QueueDel(shard);
            _resources.AddChemicals(ent.Owner, FixedPoint2.New(args.ChemicalCost));
            return;
        }

        args.Handled = true;
        PopupMutation(ent.Owner, "changeling-bone-shard-created");
    }

    private void OnResonantShriek(Entity<ChangelingResourceComponent> ent,
        ref ChangelingResonantShriekActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            !float.IsFinite(args.Radius) ||
            args.Radius < 0f ||
            args.StunTime < TimeSpan.Zero ||
            !TrySpend(ent, args.ChemicalCost))
            return;

        args.Handled = true;
        PlayMutationShriekSound(ent.Owner);
        PopupMutation(ent.Owner, "changeling-resonant-shriek-used");
        foreach (var target in _lookup.GetEntitiesInRange(ent.Owner, args.Radius))
        {
            if (target == ent.Owner)
                continue;

            if (HasComp<MobStateComponent>(target))
            {
                _stun.TryUpdateStunDuration(target, args.StunTime);
                _stun.TryKnockdown(target, args.StunTime, force: true);
            }

            if (TryComp<PoweredLightComponent>(target, out var light))
                _poweredLight.TryDestroyBulb(target, light, ent.Owner);
        }
    }

    private void OnSwapForms(Entity<ChangelingResourceComponent> ent, ref ChangelingSwapFormsActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            args.Target == ent.Owner ||
            HasComp<ChangelingResourceComponent>(args.Target) ||
            HasComp<StoreComponent>(args.Target) ||
            !HasComp<HumanoidProfileComponent>(args.Target) ||
            !TryComp<MobStateComponent>(args.Target, out var targetMobState) ||
            !_mobState.IsAlive(args.Target, targetMobState) ||
            !_mind.TryGetMind(ent.Owner, out var performerMind, out _) ||
            !_mind.TryGetMind(args.Target, out var targetMind, out _) ||
            !CanTransferChangelingBody(ent, args.Target) ||
            args.StunTime < TimeSpan.Zero)
            return;

        var targetGenome = _identities.GetGenomeId(args.Target);
        if (targetGenome == null || !TrySpend(ent, args.ChemicalCost))
            return;

        if (!TransferChangelingBody(ent, args.Target, targetGenome))
        {
            _resources.AddChemicals(ent.Owner, FixedPoint2.New(args.ChemicalCost));
            return;
        }

        args.Handled = true;
        _mind.TransferTo(targetMind, null, createGhost: false);
        _mind.TransferTo(performerMind, args.Target);
        _mind.TransferTo(targetMind, ent.Owner);

        _stun.TryUpdateParalyzeDuration(args.Target, args.StunTime);
        _stun.TryUpdateParalyzeDuration(ent.Owner, args.StunTime);
    }
}
