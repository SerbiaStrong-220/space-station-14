// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.SS220.Movement.Components;
using Content.Shared.StatusEffect;

namespace Content.Shared.SS220.Movement.Systems;

public sealed class TemporaryWaddleSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string WaddlingStatusEffect = "TemporaryWaddle";

    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryWaddleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TemporaryWaddleComponent, ComponentRemove>(OnRemoved);
    }

    private void OnInit(EntityUid uid, TemporaryWaddleComponent component, ComponentInit args)
    {
        EnsureComp<WaddleAnimationComponent>(uid, out var waddleAnimComp);

        //Set values from TemporaryWaddleComponent if entity hasn`t clown shoes or hasn`t shoes in that slot
        if (!_inventorySystem.TryGetSlotEntity(uid, "shoes", out var shoesUid) || !HasComp<WaddleWhenWornComponent>(shoesUid))
        {
            SetAnimationValues(uid);
        }
    }

    private void OnRemoved(EntityUid uid, TemporaryWaddleComponent component, ComponentRemove args)
    {
        // If uid has clown shoes, then the WaddleAnimation doesn`t removed
        if (_inventorySystem.TryGetSlotEntity(uid, "shoes", out var shoesUid) && HasComp<WaddleWhenWornComponent>(shoesUid))
            return;

        RemComp<WaddleAnimationComponent>(uid);
    }

    public void SetAnimationValues(EntityUid uid, TemporaryWaddleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<WaddleAnimationComponent>(uid, out var waddleAnimComp))
            return;

        waddleAnimComp.AnimationLength = component.AnimationLength;
        waddleAnimComp.HopIntensity = component.HopIntensity;
        waddleAnimComp.RunAnimationLengthMultiplier = component.RunAnimationLengthMultiplier;
        waddleAnimComp.TumbleIntensity = component.TumbleIntensity;
    }
}
