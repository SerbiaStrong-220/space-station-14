using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared.SS220.SpeciesWearRestriction;

public sealed class SpeciesWearRestrictionSystem : EntitySystem
{
    // ReSharper disable once MemberCanBePrivate.Global
    // remove when use in another system
    public const SlotFlags ClothingSlots =
        SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING | SlotFlags.MASK |
        SlotFlags.HEAD | SlotFlags.FEET | SlotFlags.LEGS | SlotFlags.GLOVES |
        SlotFlags.NECK | SlotFlags.EARS | SlotFlags.EYES;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpeciesWearRestrictionComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
    }

    private void OnEquipAttempt(Entity<SpeciesWearRestrictionComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if ((args.SlotFlags & ClothingSlots) == 0)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.EquipTarget, out var huAp))
        {
            TrySetReason(ent, ref args);
            args.Cancel();
            return;
        }

        var species = huAp.Species;

        if (ent.Comp.RestrictedSpecies.Count > 0 && ent.Comp.RestrictedSpecies.Contains(species))
        {
            TrySetReason(ent, ref args);
            args.Cancel();
            return;
        }

        if (ent.Comp.AllowedSpecies.Count > 0 && !ent.Comp.AllowedSpecies.Contains(species))
        {
            TrySetReason(ent, ref args);
            args.Cancel();
        }
    }

    private void TrySetReason(Entity<SpeciesWearRestrictionComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!string.IsNullOrEmpty(ent.Comp.FailedEquipPopup))
            args.Reason = Loc.GetString(ent.Comp.FailedEquipPopup);
    }
}
