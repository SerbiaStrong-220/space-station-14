// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System;
using System.Linq;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

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
    }

    private void OnInit(Entity<CultPondComponent> uid, ref MapInitEvent args)
    {
        uid.Comp.NextCharge = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CultPondComponent>();

        while (query.MoveNext(out var uid, out var pondComponent))
        {
            if (pondComponent.NextCharge == null)
                continue;

            if (pondComponent.NextCharge > _timing.CurTime)
                continue;

            if (!TryComp<SolutionContainerManagerComponent>(uid, out var comp) ||
                !_solutionContainers.TryGetSolution((uid, comp),
                    pondComponent.Solution,
                    out var soln,
                    out var solution))
                continue;

            if (pondComponent.Reagent == null)
            {
                if (solution.Contents.Count == 0)
                    continue;
                pondComponent.Reagent = solution.Contents.FirstOrDefault();
            }

            pondComponent.NextCharge = _timing.CurTime + TimeSpan.FromSeconds(pondComponent.RefillCooldown);

            if (solution.MaxVolume == solution.Volume)
                continue;

            var realTransferAmount =
                FixedPoint2.Min(pondComponent.AmmountToAdd, solution.AvailableVolume);

            solution.AddReagent(pondComponent.Reagent.Value.Reagent, realTransferAmount);

            _solutionContainers.UpdateChemicals(soln.Value);

            if (pondComponent.RechargeSound != null)
                _audio.PlayPvs(pondComponent.RechargeSound, uid);
        }
    }
}
