// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Systems;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.SS220.Medicine.Injury;
using Content.Shared.SS220.Medicine.Injury.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Medicine.Injury.Systems;

public sealed partial class InjurySystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InjuriesContainerComponent, InjuryAddedEvent>(OnInjureAdded);
        SubscribeLocalEvent<InjuriesContainerComponent, InjuryRemovedEvent>(OnInjureRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InjuriesContainerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Injuries.Count == 0 )
                continue;
            if (comp.NextTick > _timing.CurTime)
                continue;

            comp.NextTick = _timing.CurTime + TimeSpan.FromSeconds(5f);
            var bodyComp = Comp<BodyPartComponent>(uid);
            foreach (var injury in comp.Injuries)
            {
                _damageable.TryChangeDamage(bodyComp.Body!.Value, Comp<InjuryComponent>(injury).Damage, true, false);
                _bloodstream.TryModifyBloodLevel(bodyComp.Body!.Value, -1f);
            }

        }
    }
    public void OnInjureAdded(EntityUid uid, InjuriesContainerComponent component, InjuryAddedEvent ev)
    {
    }
    public void OnInjureRemoved(EntityUid uid, InjuriesContainerComponent component, InjuryRemovedEvent ev)
    {
    }

}