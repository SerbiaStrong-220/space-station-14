using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Unit;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.SS220.Maths;
using Content.Shared.Stunnable;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using SharedFelinidPipecrawlSystem = Content.Shared.SS220.Felinid.FelinidPipecrawlSystem;

namespace Content.Server.SS220.Felinid;

public sealed class FelinidHeavyGunRecoilSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FelinidHeavyGunRecoilComponent, FelinidRecoilStartedEvent>(OnRecoilStarted);
    }

    private void OnRecoilStarted(Entity<FelinidHeavyGunRecoilComponent> ent, ref FelinidRecoilStartedEvent args)
    {
        var knockdownChance = args.Profile.KnockdownChance *
                              MathF.Max(0f, ent.Comp.KnockdownChanceModifier) *
                              GetKnockdownChanceModifier(ent.Owner);
        if (_random.Prob(knockdownChance))
            _stun.TryKnockdown(ent.Owner, args.Profile.KnockdownTime, drop: true);
    }

    private float GetKnockdownChanceModifier(EntityUid uid)
    {
        var modifier = 1f;

        if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var outer) &&
            _tags.HasTag(outer.Value, HardsuitTag))
        {
            modifier *= 0.4f;
        }

        if (_inventory.TryGetSlotEntity(uid, "shoes", out var shoes) &&
            HasComp<NoSlipComponent>(shoes))
        {
            modifier *= 0.6f;
        }

        return modifier;
    }
}
