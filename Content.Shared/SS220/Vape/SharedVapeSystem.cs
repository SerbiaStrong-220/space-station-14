using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
// ReSharper disable InvertIf

namespace Content.Shared.SS220.Vape;

public abstract class SharedVapeSystem : EntitySystem
{
    [Dependency] protected readonly SharedSolutionContainerSystem Solution = default!;
    [Dependency] protected readonly FlavorProfileSystem Flavor = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly DamageableSystem Damage = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VapeComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<VapeComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<VapeComponent, EntInsertedIntoContainerMessage>(OnInsertInSlot);
        SubscribeLocalEvent<VapeComponent, EntRemovedFromContainerMessage>(OnRemoveFromSlot);
        SubscribeLocalEvent<VapeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<VapeComponent, GotEmaggedEvent>(OnGotEmagged);

        SubscribeLocalEvent<VapePartComponent, MapInitEvent>(OnMapInit);
    }

    private void OnCompRemove(Entity<VapeComponent> ent, ref ComponentRemove _)
    {
        Audio.Stop(ent.Comp.SoundEntity); // just for lucky
    }

    private void OnEquipAttempt(Entity<VapeComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!_ingestion.HasMouthAvailable(args.EquipTarget, ent))
        {
            args.Reason = Loc.GetString("vape-no-mouth-available");
            args.Cancel();
            return;
        }

        if (ent.Comp.CartridgeEntity == null)
        {
            args.Reason = Loc.GetString("vape-no-cartridge");
            args.Cancel();
            return;
        }

        if (ent.Comp.AtomizerEntity == null || !Solution.TryGetRefillableSolution(ent.Comp.AtomizerEntity.Value, out _, out var sol))
        {
            args.Reason = Loc.GetString("vape-no-solution-or-atomizer");
            args.Cancel();
        }
    }

    private void OnInsertInSlot(Entity<VapeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdatePartSlot(ent, args.Entity, true);
    }

    private void OnRemoveFromSlot(Entity<VapeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdatePartSlot(ent, args.Entity, false);
    }

    private void OnExamined(Entity<VapeComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp<VapePartComponent>(ent.Comp.CartridgeEntity, out var cartPart) &&
            cartPart.PartType is CartridgePartData cartridge)
        {
            var durability = MathF.Round(cartridge.CurrentDurability / cartridge.MaxDurability * 100f);
            args.PushMarkup(Loc.GetString("vape-examine-cartridge-durability", ("currentDurability", durability)));
        }
    }

    private void OnGotEmagged(Entity<VapeComponent> ent, ref GotEmaggedEvent args)
    {
        if (ent.Comp.AtomizerEntity == null || ent.Comp.CartridgeEntity == null || ent.Comp.IsEmagged)
            return;

        if (!Solution.TryGetRefillableSolution(ent.Comp.AtomizerEntity.Value, out _, out var sol))
            return;

        if (!TryComp<VapePartComponent>(ent.Comp.AtomizerEntity.Value, out var vapePart))
            return;

        if (vapePart.PartType is not AtomizerPartData atomizer)
            return;

        if (atomizer.EmaggedVolume.HasValue)
            sol.MaxVolume = atomizer.EmaggedVolume.Value;

        args.Type = EmagType.Interaction;
        args.Handled = true;

        ent.Comp.IsEmagged = true;
        Dirty(ent);
    }

    private void OnMapInit(Entity<VapePartComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.PartType is not { } data)
            return;

        data.CurrentDurability = data.MaxDurability;
        Dirty(ent);
    }

    private void UpdatePartSlot(Entity<VapeComponent> ent, EntityUid entity, bool inserted)
    {
        if (!TryComp<VapePartComponent>(entity, out var vapePart))
            return;

        switch (vapePart.PartType)
        {
            case AtomizerPartData:
                ent.Comp.AtomizerEntity = inserted ? entity : null;
                _appearance.SetData(ent, VapeParts.Atomizer, inserted);
                break;

            case CartridgePartData:
                ent.Comp.CartridgeEntity = inserted ? entity : null;
                _appearance.SetData(ent, VapeParts.Cartridge, inserted);
                break;
        }

        ent.Comp.Puffing = false;
        ent.Comp.SoundEntity = Audio.Stop(ent.Comp.SoundEntity);
        ent.Comp.StartPuffingTime = null;
        Dirty(ent);
    }
}
