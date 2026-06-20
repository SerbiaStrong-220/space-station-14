// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction.Events;
using Content.Shared.SS220.SmokeCenser;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Server.Popups;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.SS220.SmokeCenser;

public sealed class CenserSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const string CenserSolutionName = "reagents";
    
    private static readonly ProtoId<ReagentPrototype> HolyWaterReagentId = "Holywater";

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<CenserComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<CenserComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_solutionContainer.TryGetSolution(entity.Owner, CenserSolutionName, out var soln, out var solution))
            return;

        var waterCost = entity.Comp.WaterCost;
        var holyWaterQuantity = FixedPoint2.Zero;
        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype != HolyWaterReagentId)
            {
                _popupSystem.PopupEntity(Loc.GetString("censer-contaminated"), entity.Owner, args.User);
                return;
            }
            
            holyWaterQuantity = reagent.Quantity;
        }

        if (holyWaterQuantity < waterCost)
        {
            _popupSystem.PopupEntity(Loc.GetString("censer-empty"), entity.Owner, args.User);
            return;
        }

        _solutionContainer.SplitSolution(soln.Value, waterCost);

        ReleaseCenserVapor(args.User, entity.Comp);


        _audio.PlayPvs(entity.Comp.SoundUse, entity.Owner);
        args.Handled = true;
    }

    private void ReleaseCenserVapor(EntityUid user, CenserComponent comp)
    {
        var environment = _atmos.GetContainingMixture(user, true, true);
        if (environment == null)
            return;

        var merger = new GasMixture(1) { Temperature = comp.Temperature };
        merger.SetMoles(comp.GasType, comp.Moles);

        _atmos.Merge(environment, merger);
    }
}
