using Content.Shared.Atmos;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server.SS220.Atmos
{
    public sealed class GasTankSelfRefillSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosSys = default!;

        public override void Initialize()
        {
            base.Initialize();
        }

        //todo make refiilment from the atmosphere
        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<GasTankSelfRefillComponent, GasTankComponent>();
            while (query.MoveNext(out var uid, out var comp, out var tank))
            {
                if (tank.IsValveOpen) //prevent air cycling
                    continue;

                if(tank.Air.Pressure >= (1000 - comp.AutoRefillRate)) //fill if its lower than max.
                    continue;
                /*
                if(_atmosSys.GetContainingMixture(uid, excite: true) == null)
                    continue;
                */

                var mixSize = tank.Air.Volume;
                var newMix = new GasMixture(mixSize);
                newMix.SetMoles(Gas.Oxygen, ((tank.Air.Pressure + comp.AutoRefillRate) * mixSize) / (Atmospherics.R * Atmospherics.T20C)); // Fill the tank
                newMix.Temperature = Atmospherics.T20C;
                tank.Air = newMix;
            }
        }
    }
}
