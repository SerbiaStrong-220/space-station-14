// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.SS220.Pathology;
using Content.Shared.Temperature.Components;
using Robust.Shared.Random;

namespace Content.Server.SS220.Pathology;

public sealed partial class PathologyEnvironmentEffectSystem : EntitySystem
{
    [Dependency] private TemperatureSystem _temperature = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureComponent, PathologyTemperatureEffectEvent>(OnTemperature);
        SubscribeLocalEvent<FlammableComponent, PathologyIgniteEffectEvent>(OnIgnite);
    }

    private void OnTemperature(Entity<TemperatureComponent> ent, ref PathologyTemperatureEffectEvent args)
    {
        if (ent.Comp.CurrentTemperature < args.Temperature)
            _temperature.ForceChangeTemperature(ent, args.Temperature, ent.Comp);
    }

    private void OnIgnite(Entity<FlammableComponent> ent, ref PathologyIgniteEffectEvent args)
    {
        if (!_random.Prob(args.Chance))
            return;

        _flammable.AdjustFireStacks(ent, args.FireStacks, ent.Comp, ignite: true);
    }
}
