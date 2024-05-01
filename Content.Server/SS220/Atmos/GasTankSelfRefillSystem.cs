using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Server.Atmos;

namespace Content.Server.SS220.Atmos
{
    public sealed class GasTankSelfRefillSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        //todo make refiilment from the atmosphere
        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<GasTankSelfRefillComponent, GasTankComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.IsValveOpen || comp.Air.Pressure >= (1000 - 0.5f))
                    continue;

                var mixSize = comp.Air.Volume;
                var newMix = new GasMixture(mixSize);
                newMix.SetMoles(Gas.Oxygen, ((comp.Air.Pressure + 0.5f) * mixSize) / (Atmospherics.R * Atmospherics.T20C)); // Fill the tank up to 1000KPA.
                newMix.Temperature = Atmospherics.T20C;
                comp.Air = newMix;
            }
        }
    }
}
