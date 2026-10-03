// SS220-glasses-on-forehead-begin
using Content.Shared.SS220.Clothing.Components;
// SS220-glasses-on-forehead-end
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class BlurryVisionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _foreheadInventory = default!; // SS220-glasses-on-forehead

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisionCorrectionComponent, GotEquippedEvent>(OnGlassesEquipped);
        SubscribeLocalEvent<VisionCorrectionComponent, GotUnequippedEvent>(OnGlassesUnequipped);
        SubscribeLocalEvent<VisionCorrectionComponent, InventoryRelayedEvent<GetBlurEvent>>(OnGetBlur);
    }

    private void OnGetBlur(Entity<VisionCorrectionComponent> glasses, ref InventoryRelayedEvent<GetBlurEvent> args)
    {
        // SS220-glasses-on-forehead-begin
        if (HasComp<GlassesOnForeheadComponent>(glasses) &&
            _foreheadInventory.InSlotWithFlags(glasses.Owner, SlotFlags.HEAD))
            return;
        // SS220-glasses-on-forehead-end

        args.Args.Blur += glasses.Comp.VisionBonus;
        args.Args.CorrectionPower *= glasses.Comp.CorrectionPower;
    }

    public void UpdateBlurMagnitude(Entity<BlindableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var ev = new GetBlurEvent(ent.Comp.EyeDamage);
        RaiseLocalEvent(ent, ev);

        var blur = Math.Clamp(ev.Blur, 0, BlurryVisionComponent.MaxMagnitude);
        if (blur <= 0)
        {
            RemCompDeferred<BlurryVisionComponent>(ent);
            return;
        }

        var blurry = EnsureComp<BlurryVisionComponent>(ent);
        blurry.Magnitude = blur;
        blurry.CorrectionPower = ev.CorrectionPower;
        Dirty(ent, blurry);
    }

    private void OnGlassesEquipped(Entity<VisionCorrectionComponent> glasses, ref GotEquippedEvent args)
    {
        UpdateBlurMagnitude(args.EquipTarget);
    }

    private void OnGlassesUnequipped(Entity<VisionCorrectionComponent> glasses, ref GotUnequippedEvent args)
    {
        UpdateBlurMagnitude(args.EquipTarget);
    }
}

public sealed class GetBlurEvent : EntityEventArgs, IInventoryRelayEvent
{
    public readonly float BaseBlur;
    public float Blur;
    public float CorrectionPower = BlurryVisionComponent.DefaultCorrectionPower;

    public GetBlurEvent(float blur)
    {
        Blur = blur;
        BaseBlur = blur;
    }

    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.EYES;
}
