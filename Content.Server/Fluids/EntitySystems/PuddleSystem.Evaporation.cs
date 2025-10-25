using Content.Server.Mind;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.SS220.RoundEndInfo;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    //ss220 add additional info for round start
    [Dependency] private readonly IRoundEndInfoManager _infoManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    //ss220 add additional info for round end

    private static readonly TimeSpan EvaporationCooldown = TimeSpan.FromSeconds(1);

    private void OnEvaporationMapInit(Entity<EvaporationComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextTick = _timing.CurTime + EvaporationCooldown;
    }

    private void UpdateEvaporation(EntityUid uid, Solution solution)
    {
        if (HasComp<EvaporationComponent>(uid))
        {
            return;
        }

        if (solution.GetTotalPrototypeQuantity(GetEvaporatingReagents(solution)) > FixedPoint2.Zero)
        {
            var evaporation = AddComp<EvaporationComponent>(uid);
            evaporation.NextTick = _timing.CurTime + EvaporationCooldown;
            return;
        }

        RemComp<EvaporationComponent>(uid);
    }

    private void TickEvaporation()
    {
        var query = EntityQueryEnumerator<EvaporationComponent, PuddleComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var evaporation, out var puddle))
        {
            if (evaporation.NextTick > curTime)
                continue;

            evaporation.NextTick += EvaporationCooldown;

            if (!_solutionContainerSystem.ResolveSolution(uid, puddle.SolutionName, ref puddle.Solution, out var puddleSolution))
                continue;

            // Yes, this means that 50u water + 50u holy water evaporates twice as fast as 100u water.
            foreach ((string evaporatingReagent, FixedPoint2 evaporatingSpeed) in GetEvaporationSpeeds(puddleSolution))
            {
                var reagentTick = evaporation.EvaporationAmount * EvaporationCooldown.TotalSeconds * evaporatingSpeed;
                puddleSolution.SplitSolutionWithOnly(reagentTick, evaporatingReagent);
            }

            // Despawn if we're done
            if (puddleSolution.Volume == FixedPoint2.Zero)
            {
                // Spawn a *sparkle*
                Spawn("PuddleSparkle", xformQuery.GetComponent(uid).Coordinates);

                //ss220 add additional info for round start
                if (puddle.LastInteractionUser != null &&
                    _mind.TryGetMind(puddle.LastInteractionUser.Value, out var mind, out _))
                {
                    _infoManager.EnsureInfo<PuddleInfo>().Record(mind);
                }
                //ss220 add additional info for round end

                QueueDel(uid);
            }

            _solutionContainerSystem.UpdateChemicals(puddle.Solution.Value);
        }
    }
}
