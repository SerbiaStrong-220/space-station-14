// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.Atmos;

public sealed partial class ItemGasSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ItemGasSpawnerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var isLimitReached = comp.LimitedSpawn && comp.RemainingAmountToSpawn <= 0;
            if (isLimitReached ||
                !GetValidEnvironment(uid, comp, out var environment))
                continue;

            var toSpawn = CapSpawnAmount(uid, comp, comp.SpawnAmount * frameTime, environment);
            if (toSpawn <= 0)
                continue;

            var merger = new GasMixture(1) { Temperature = comp.SpawnTemperature };
            merger.SetMoles(comp.SpawnGas, toSpawn);
            _atmosphere.Merge(environment, merger);

            if (comp.LimitedSpawn)
                comp.RemainingAmountToSpawn = MathF.Max(0, comp.RemainingAmountToSpawn - toSpawn);
        }
    }

    private bool GetValidEnvironment(EntityUid uid, ItemGasSpawnerComponent component, [NotNullWhen(true)] out GasMixture? environment)
    {
        var transform = Transform(uid);
        var position = _transform.GetGridOrMapTilePosition(uid, transform);

        // Treat space as an invalid environment
        if (_atmosphere.IsTileSpace(transform.GridUid, transform.MapUid, position))
        {
            environment = null;
            return false;
        }

        environment = _atmosphere.GetContainingMixture((uid, transform), true, true);
        return environment != null;
    }

    private float CapSpawnAmount(EntityUid uid, ItemGasSpawnerComponent component, float toSpawnTarget, GasMixture environment)
    {
        // How many moles could we theoretically spawn. Cap by pressure and amount.
        var allowableMoles = Math.Min(
            (component.MaxExternalPressure - environment.Pressure) * environment.Volume / (component.SpawnTemperature * Atmospherics.R),
            component.MaxExternalAmount - environment.TotalMoles);

        var toSpawnReal = Math.Clamp(allowableMoles, 0f, toSpawnTarget);

        if (toSpawnReal < Atmospherics.GasMinMoles)
            return 0f;

        return toSpawnReal;
    }
}
