using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Body.Events;

namespace Content.Shared.Mobs.Systems;

public partial class MobStateSystem
{
    private void OnProcessThermalRegulationAttempt(Entity<MobStateComponent> ent, ref ProcessThermalRegulationAttemptEvent args)
    {
        if (IsDead(ent))
            args.Cancel();
    }
}
