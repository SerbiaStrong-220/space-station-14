// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition.Components;
using Content.Shared.SS220.Smoking;
using Content.Shared.Smoking;
using Robust.Shared.Timing;

namespace Content.Server.Nutrition.EntitySystems;

public sealed partial class SmokingSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    private void InitializeSmokablePuff()
    {
        SubscribeLocalEvent<SmokablePuffComponent, SmokablePuffActionEvent>(OnSmokablePuffAction);
        SubscribeLocalEvent<SmokablePuffComponent, SmokablePuffDoAfterEvent>(OnSmokablePuffDoAfter);
    }

    private void OnSmokablePuffAction(Entity<SmokablePuffComponent> entity, ref SmokablePuffActionEvent args)
    {
        var user = args.Performer;

        if (!CanPuff(entity, user))
            return;

        if (_doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            user,
            entity.Comp.PuffDelay,
            new SmokablePuffDoAfterEvent(),
            entity.Owner,
            target: user,
            used: entity.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            CancelDuplicate = false,
        }))
        {
            _audio.PlayPvs(entity.Comp.PuffInhaleSound, entity.Owner);
            _actions.SetUseDelay((args.Action, args.Action), entity.Comp.PuffDelay + entity.Comp.PuffCooldown);
        }

        args.Handled = true;
    }

    private void OnSmokablePuffDoAfter(Entity<SmokablePuffComponent> entity, ref SmokablePuffDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var user = args.Args.Target.Value;

        var environment = _atmos.GetContainingMixture(user, true, true);
        if (environment == null || !CanPuff(entity, user))
            return;

        if (!TryComp(entity, out SmokableComponent? smokable)
            || !_solutionContainerSystem.TryGetSolution(entity.Owner,
                smokable.Solution,
                out var soln,
                out var solution))
        {
            return;
        }

        var inhaled = _solutionContainerSystem.SplitSolution(soln.Value, entity.Comp.PuffCost);

        entity.Comp.NextPuffTime = _timing.CurTime + entity.Comp.PuffCooldown;

        if (inhaled.Volume > 0 && TryComp(user, out BloodstreamComponent? bloodstream))
        {
            _reactiveSystem.DoEntityReaction(user, inhaled, ReactionMethod.Ingestion);
            _bloodstreamSystem.TryAddToBloodstream((user, bloodstream), inhaled);
        }

        ReleaseVapor(entity.Comp, environment);

        if (solution.Volume <= 0)
            RaiseLocalEvent(entity.Owner, new SmokableSolutionEmptyEvent(), true);
    }

    private bool CanPuff(Entity<SmokablePuffComponent> entity, EntityUid user)
    {
        if (!TryComp(entity, out SmokableComponent? smokable))
            return false;

        if (smokable.State != SmokableState.Lit)
        {
            if (smokable.State == SmokableState.Unlit)
                _popupSystem.PopupEntity(Loc.GetString("smokable-puff-not-lit"), entity.Owner, user);

            return false;
        }

        if (_timing.CurTime < entity.Comp.NextPuffTime)
            return false;

        if (!_inventorySystem.TryGetSlotEntity(user, "mask", out var inMouth) || inMouth != entity.Owner)
            return false;

        if (!HasComp<BloodstreamComponent>(user) || !_ingestion.HasMouthAvailable(user, user))
            return false;

        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, smokable.Solution, out _, out var solution)
            || solution.Volume <= 0)
            return false;

        return true;
    }

    private void ReleaseVapor(SmokablePuffComponent comp, GasMixture environment)
    {
        var merger = new GasMixture(1) { Temperature = comp.Temperature };
        merger.SetMoles(comp.GasType, comp.Moles);

        _atmos.Merge(environment, merger);
    }
}