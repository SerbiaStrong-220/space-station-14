// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Medical;

namespace Content.Shared.SS220.DirectCompactDefibrillator;

public sealed class DirectCompactDefibrillatorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DirectCompactDefibrillatorComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<DirectCompactDefibrillatorComponent> entity, ref ComponentInit _)
    {
        if (!HasComp<DefibrillatorComponent>(entity))
            Log.Error($"Entity {ToPrettyString(entity)} with {nameof(DirectCompactDefibrillatorComponent)} don't have {nameof(DefibrillatorComponent)}, so it wont work. Add it!");
    }
}
