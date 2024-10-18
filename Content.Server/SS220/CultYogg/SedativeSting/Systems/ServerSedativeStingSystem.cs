using System.Linq;
using Content.Server.Body.Components;
using Content.Server.SS220.Autoinjector;
using Content.Server.SS220.CultYogg.CultPond.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.SedativeSting.Components;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.Audio;

namespace Content.Server.SS220.CultYogg.SedativeSting.Systems;

public sealed class ServerSedativeStingSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SedativeStingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SedativeStingComponent, MeleeHitEvent>(OnAttack);
        SubscribeLocalEvent<SedativeStingComponent, UseInHandEvent>(OnUseInHand);
    }

    private bool TryUse(Entity<SedativeStingComponent> entity, EntityUid target, EntityUid user)
    {
        if (HasComp<CultPondComponent>(target)
            && _solutionContainers.TryGetDrawableSolution(target, out var drawableSolution, out _) //Это для проверки откуда берем
            )
        {
            return TryDraw(entity, target, drawableSolution.Value, user);
        }

        return TryDoInject(entity, target, user);
    }

    private void OnUseInHand(Entity<SedativeStingComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryDoInject(entity, args.User, args.User);
    }

    private void OnAfterInteract(Entity<SedativeStingComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = TryUse(entity, args.Target.Value, args.User);
    }

    private void OnAttack(Entity<SedativeStingComponent> entity, ref MeleeHitEvent args)
    {
        if (!args.HitEntities.Any())
            return;

        TryDoInject(entity, args.HitEntities[0], args.User);
    }

    private bool TryDoInject(Entity<SedativeStingComponent> entity, EntityUid target, EntityUid user)
    {
        var (uid, component) = entity;

        if (!HasComp<SolutionContainerManagerComponent>(target) ||
            !HasComp<MobStateComponent>(target))
            return false;

        if (TryComp(uid, out UseDelayComponent? delayComp))
        {
            if (_useDelay.IsDelayed((uid, delayComp)))
                return false;
        }

        if (HasComp<NeedleProtectionComponent>(target) && !component.IgnoreProtection)
        {
            _popup.PopupEntity(Loc.GetString("loc-hypo-protection-popup"), entity, user);
            return false;
        }

        if (_inventory.TryGetSlots(target, out var slots) && !component.IgnoreProtection)
        {
            foreach (var slot in slots)
            {
                if (_inventory.TryGetSlotEntity(target, slot.Name, out var item) && HasComp<NeedleProtectionComponent>(item))
                {
                    _popup.PopupEntity(Loc.GetString("loc-hypo-protection-popup"), entity, user);
                    return false;
                }
            }
        }

        string? msgFormat = null;

        if (target == user)
            msgFormat = "hypospray-component-inject-self-message";

        if (!_solutionContainers.TryGetSolution(uid, component.SolutionName, out var soln, out var solution) || solution.Volume == 0)
        {
            _popup.PopupEntity(Loc.GetString("hypospray-component-empty-message"), target, user);
            return true;
        }

        if (!_solutionContainers.TryGetInjectableSolution(target, out var targetSoln, out var targetSolution))
        {
            _popup.PopupEntity(Loc.GetString("hypospray-cant-inject", ("target", Identity.Entity(target, EntityManager))), target, user);
            return false;
        }

        _popup.PopupEntity(Loc.GetString(msgFormat ?? "hypospray-component-inject-other-message", ("other", target)), target, user);

        if (target != user)
        {
            _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
        }

        _audio.PlayPvs(component.InjectSound, user);

        if (delayComp != null)
            _useDelay.TryResetDelay((uid, delayComp));

        var realTransferAmount = FixedPoint2.Min(component.TransferAmount, targetSolution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("hypospray-component-transfer-already-full-message", ("owner", target)), target, user);
            return true;
        }

        var removedSolution = _solutionContainers.SplitSolution(soln.Value, realTransferAmount);

        if (!targetSolution.CanAddSolution(removedSolution))
            return true;
        _reactiveSystem.DoEntityReaction(target, removedSolution, ReactionMethod.Injection);
        _solutionContainers.TryAddSolution(targetSoln.Value, removedSolution);

        var ev = new TransferDnaEvent { Donor = target, Recipient = uid };
        RaiseLocalEvent(target, ref ev);

        _adminLogger.Add(LogType.ForceFeed, $"{EntityManager.ToPrettyString(user):user} injected {EntityManager.ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {EntityManager.ToPrettyString(uid):using}");

        return true;
    }

    private bool TryDraw(Entity<SedativeStingComponent> entity, Entity<BloodstreamComponent?> target, Entity<SolutionComponent> targetSolution, EntityUid user)
    {
        if (!_solutionContainers.TryGetSolution(entity.Owner,
                entity.Comp.SolutionName,
                out var soln,
                out var solution) || solution.AvailableVolume == 0)
        {
            return false;
        }

        var realTransferAmount = FixedPoint2.Min(entity.Comp.TransferAmount,
            targetSolution.Comp.Solution.Volume,
            solution.AvailableVolume);

        if (realTransferAmount <= 0)
        {
            _popup.PopupEntity(
                Loc.GetString("injector-component-target-is-empty-message",
                    ("target", Identity.Entity(target, EntityManager))),
                entity.Owner,
                user);
            return false;
        }

        var removedSolution = _solutionContainers.Draw(target.Owner, targetSolution, realTransferAmount);

        if (!_solutionContainers.TryAddSolution(soln.Value, removedSolution))
        {
            return false;
        }

        _popup.PopupEntity(Loc.GetString("injector-component-draw-better-success-message",
            ("amount", removedSolution.Volume),
            ("reagent", removedSolution.Contents[0].Reagent),
            ("target", Identity.Entity(target, EntityManager))),
            entity.Owner,
            user);
        return true;
    }
}
