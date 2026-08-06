// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Damage.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.AltArmor;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.InternalComponentDamageRelay;

public sealed partial class InternalComponentDamageRelaySystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private AltArmorSystem _altArmor = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InternalComponentDamageRelayComponent, DamageModifyEvent>(OnDamageChange);
    }

    public void OnDamageChange(Entity<InternalComponentDamageRelayComponent> ent, ref DamageModifyEvent args)
    {
        _altArmor.ModifyDamage(ent.Owner, args.OriginalDamage, out var resultDamage, out var resultArmorDamage);

        args.Damage = resultArmorDamage;

        if (ent.Comp.Containers == string.Empty)
            return;

        if (!args.OriginalDamage.AnyPositive() &&
            !ent.Comp.ApplyNegative)
            return;

        if (!TryGetNetEntity(ent.Owner, out var netEnt))
            return;

        var rand = new System.Random(_gameTiming.CurTick.GetHashCode() + netEnt.Value.Id);

        var containerID = _prototype.Index(ent.Comp.Containers).Pick(rand);

        if (containerID == "None")
            return;

        if (!_container.TryGetContainer(ent.Owner, containerID, out var container) ||
            container is not ContainerSlot containerSlot ||
            containerSlot.ContainedEntity is not { Valid: true } internalComponent)
            return;

        _damageable.TryChangeDamage(internalComponent, resultDamage);
    }
}
