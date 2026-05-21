using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Content.Shared.Censer;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Censer;

public sealed class CenserSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CenserComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, CenserComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _delay.IsDelayed((uid, useDelay)))
            return;

        if (!_solutionContainer.TryGetSolution(uid, "reagents", out var soln, out var solution))
            return;

        var vaporCost = FixedPoint2.New(component.VaporAmount);
        var hasHolyWater = false;
        var holyWaterQuantity = FixedPoint2.Zero;
        var isContaminated = false;

        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype == "Holywater")
            {
                hasHolyWater = true;
                holyWaterQuantity = reagent.Quantity;
            }
            else
            {
                isContaminated = true;
                break;
            }
        }

        if (isContaminated)
        {
            _popupSystem.PopupEntity(Loc.GetString("censer-contaminated"), uid, args.User);
            return;
        }

        if (!hasHolyWater || holyWaterQuantity < vaporCost)
        {
            _popupSystem.PopupEntity(Loc.GetString("censer-empty"), uid, args.User);
            return;
        }

        _solutionContainer.SplitSolution(soln.Value, vaporCost);

        ReleaseCenserVapor(args.User, component);

        if (component.SoundUse != null)
        {
            _audio.PlayPvs(component.SoundUse, uid);
        }

        _delay.TryResetDelay((uid, useDelay));
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