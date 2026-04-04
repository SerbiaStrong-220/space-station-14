// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.SS220.CultYogg.Pond;

public sealed class CultPondSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultPondComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<CultPondComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnInit(Entity<CultPondComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextCharge = _timing.CurTime;
        UpdateIsFilled(entity);
    }

    private void OnSolutionChanged(Entity<CultPondComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (entity.Comp.Solution != args.SolutionId)
            return;

        // if the solution was draw from the pond when it was full, then the refill cooldown starts from that moment
        if (entity.Comp.IsFilled && args.Solution.AvailableVolume > 0)
            entity.Comp.NextCharge = _timing.CurTime + entity.Comp.RefillCooldown;

        UpdateIsFilled(entity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CultPondComponent>();

        while (query.MoveNext(out var uid, out var pondComp))
        {
            if (pondComp.NextCharge == null)
                continue;

            if (pondComp.NextCharge > _timing.CurTime)
                continue;

            if (!TryComp<SolutionContainerManagerComponent>(uid, out var comp) ||
                !_solutionContainers.TryGetSolution((uid, comp),
                    pondComp.Solution,
                    out var soln,
                    out var solution))
                continue;

            if (pondComp.Reagent == null)
            {
                if (solution.Contents.Count == 0)
                    continue;
                pondComp.Reagent = solution.Contents.FirstOrDefault();
            }

            pondComp.NextCharge = _timing.CurTime + pondComp.RefillCooldown;

            if (solution.MaxVolume == solution.Volume)
                continue;

            var realTransferAmount =
                FixedPoint2.Min(pondComp.AmountToAdd, solution.AvailableVolume);

            solution.AddReagent(pondComp.Reagent.Value.Reagent, realTransferAmount);

            _solutionContainers.UpdateChemicals(soln.Value);

            if (pondComp.RechargeSound != null)
                _audio.PlayPvs(pondComp.RechargeSound, uid);
        }
    }

    private void UpdateIsFilled(Entity<CultPondComponent> entity)
    {
        var (uid, comp) = entity;
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionContainer) ||
            !_solutionContainers.TryGetSolution((uid, solutionContainer), comp.Solution, out _, out var soln))
            return;

        entity.Comp.IsFilled = soln.AvailableVolume <= 0;
    }
}
