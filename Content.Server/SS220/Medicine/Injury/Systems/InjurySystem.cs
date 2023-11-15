// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.SS220.Medicine.Injury;
using Content.Shared.SS220.Medicine.Injury.Components;

namespace Content.Server.SS220.Medicine.Injury.Systems;

public sealed partial class InjurySystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

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

        while (query.MoveNext(out var uid, out var injuriesContainer))
        {
            foreach (var innerInjuries in injuriesContainer.InnerInjuries)
            {
                var injuryComp = Comp<InjuryComponent>(innerInjuries);
                foreach (var damageSpecifier in injuryComp.DamageSpecifiers)
                {
                    // _damageable.TryChangeDamage(uid, damageSpecifier, true);
                }
            }

            foreach (var outerInjuries in injuriesContainer.OuterInjuries)
            {

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