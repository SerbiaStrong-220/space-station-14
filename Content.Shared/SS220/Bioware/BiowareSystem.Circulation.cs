// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Body.Events;
using Content.Shared.Metabolism;
using Content.Shared.SS220.Metabolism;

namespace Content.Shared.SS220.Bioware;

public sealed partial class BiowareSystem
{
    [Dependency] private MetabolizerSystem _metabolizer = default!;

    private void InitializeCirculation()
    {
        SubscribeLocalEvent<VeinThreadingComponent, BleedModifierEvent>(OnVeinThreadingBleed);

        SubscribeLocalEvent<CapillaryWeavingComponent, MapInitEvent>(OnCapillaryWeavingMapInit);
        SubscribeLocalEvent<CapillaryWeavingComponent, GetMetabolizerRateEvent>(OnCapillaryWeavingRate);
    }

    private void OnVeinThreadingBleed(Entity<VeinThreadingComponent> ent, ref BleedModifierEvent args)
    {
        args.BleedAmount *= ent.Comp.BleedCoefficient;
    }

    private void OnCapillaryWeavingMapInit(Entity<CapillaryWeavingComponent> ent, ref MapInitEvent args)
    {
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnCapillaryWeavingRate(Entity<CapillaryWeavingComponent> ent, ref GetMetabolizerRateEvent args)
    {
        if (args.Stages.Contains(ent.Comp.Stage))
            args.Multiplier *= ent.Comp.RateMultiplier;
    }
}
