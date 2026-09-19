using Content.Shared.Inventory;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Stunnable;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Felinid;

public sealed partial class GunRecoilModifierSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunRecoilModifierComponent, GunRecoilStartedEvent>(OnRecoilStarted);
    }

    private void OnRecoilStarted(Entity<GunRecoilModifierComponent> ent, ref GunRecoilStartedEvent args)
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
            HasComp<NoSlipComponent>(shoes.Value))
        {
            modifier *= 0.6f;
        }

        return modifier;
    }
}
