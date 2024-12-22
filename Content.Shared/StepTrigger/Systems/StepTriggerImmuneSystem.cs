using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Tag;

namespace Content.Shared.StepTrigger.Systems;

public sealed class StepTriggerImmuneSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        // SubscribeLocalEvent<StepTriggerImmuneComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt); 220 borg shard fix
        SubscribeLocalEvent<ClothingRequiredStepTriggerComponent, StepTriggerAttemptEvent>(OnStepTriggerClothingAttempt);
        SubscribeLocalEvent<ClothingRequiredStepTriggerComponent, ExaminedEvent>(OnExamined);
    }
    // start 220 borg shard fix
    // private void OnStepTriggerAttempt(Entity<StepTriggerImmuneComponent> ent, ref StepTriggerAttemptEvent args)
    // {
    //     args.Cancelled = true;
    // }
    // end 220 borg shard fix
    private void OnStepTriggerClothingAttempt(EntityUid uid, ClothingRequiredStepTriggerComponent component, ref StepTriggerAttemptEvent args)
    {
        if (_inventory.TryGetInventoryEntity<ClothingRequiredStepTriggerImmuneComponent>(args.Tripper, out _)
        || TryComp<StepTriggerImmuneComponent>(args.Tripper, out _)) // 220 borg shard fix
        {
            args.Cancelled = true;
        }
    }

    private void OnExamined(EntityUid uid, ClothingRequiredStepTriggerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("clothing-required-step-trigger-examine"));
    }
}
