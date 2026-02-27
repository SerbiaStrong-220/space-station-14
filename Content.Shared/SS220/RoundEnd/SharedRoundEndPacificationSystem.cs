// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Explosion.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Prototypes;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.RoundEnd;

public abstract class SharedRoundEndPacifiedSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool CheckInteraction(Entity<RoundEndPacifiedComponent> user, EntityUid item)
    {
        if (HasComp<FlashComponent>(item) ||
            HasComp<SmokeOnTriggerComponent>(item) ||
            HasComp<TwoStageTriggerComponent>(item))
        {
            return false;
        }

        // Allow only toy explosives
        if (TryComp<ExplosiveComponent>(item, out var explosive))
        {
            if (explosive.TotalIntensity > 3 || explosive.IntensitySlope > 3 || explosive.CanCreateVacuum)
            {
                return false;
            }
        }

        // Allow soap and foam dart grenades
        if (TryComp<ScatteringGrenadeComponent>(item, out var grenade) && grenade.FillPrototype is not null)
        {
            var fill = _prototypeManager.Index(grenade.FillPrototype.Value);
            if (fill.HasComponent<ExplosiveComponent>())
            {
                return false;
            }
        }

        return true;
    }
}
