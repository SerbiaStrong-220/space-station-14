// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory.Events;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Radio.Components;
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Language.Systems;

public sealed partial class LanguageEncryptionKeySystem : EntitySystem
{
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    private const float EmptyChargeLevel = 0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageEncryptionKeyComponent, EntGotInsertedIntoContainerMessage>(OnKeyInserted);
        SubscribeLocalEvent<LanguageEncryptionKeyComponent, EntGotRemovedFromContainerMessage>(OnKeyRemoved);
        SubscribeLocalEvent<GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<PowerCellSlotComponent, PowerCellChangedEvent>(OnHolderPowerCellChanged);
        SubscribeLocalEvent<PowerCellSlotComponent, PowerCellSlotEmptyEvent>(OnHolderPowerEmpty);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, BatteryStateChangedEvent>(OnHolderBatteryStateChanged);
    }

    private void OnKeyInserted(Entity<LanguageEncryptionKeyComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != EncryptionKeyHolderComponent.KeyContainerName)
            return;

        if (!TryGetWearer(args.Container.Owner, out var wearer))
            return;

        if (!TryComp<LanguageComponent>(wearer, out var langComp))
            return;

        if (TryComp<PowerCellSlotComponent>(args.Container.Owner, out var slotComp)
            && !HasPower((args.Container.Owner, slotComp)))
            return;

        _language.AddLanguages((wearer.Value, langComp), ent.Comp.Languages, canSpeak: false);
    }

    private void OnKeyRemoved(Entity<LanguageEncryptionKeyComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != EncryptionKeyHolderComponent.KeyContainerName)
            return;

        if (!TryGetWearer(args.Container.Owner, out var wearer))
            return;

        if (!TryComp<LanguageComponent>(wearer, out var langComp))
            return;

        foreach (var language in ent.Comp.Languages)
            RemoveKeyLanguage((wearer.Value, langComp), language);
    }

    private void OnEquipped(GotEquippedEvent args)
    {
        if (!HasComp<HeadsetComponent>(args.Equipment))
            return;

        AddLanguagesFromHeadset(args.Equipment, args.EquipTarget);
    }

    private void OnUnequipped(GotUnequippedEvent args)
    {
        if (!HasComp<HeadsetComponent>(args.Equipment))
            return;

        RemoveLanguagesFromHeadset(args.Equipment, args.EquipTarget);
    }

    private void OnHolderPowerCellChanged(Entity<PowerCellSlotComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected)
            return;

        if (!TryComp<EncryptionKeyHolderComponent>(ent.Owner, out var holder))
            return;

        if (!HasPower(ent.AsNullable()))
            return;

        AddLanguagesFromHolder((ent.Owner, holder));
    }

    private void OnHolderPowerEmpty(Entity<PowerCellSlotComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (!TryComp<EncryptionKeyHolderComponent>(ent.Owner, out var holder))
            return;

        RemoveLanguagesFromHolder((ent.Owner, holder));
    }

    private void OnHolderBatteryStateChanged(Entity<EncryptionKeyHolderComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.OldState != BatteryState.Empty || args.NewState == BatteryState.Empty)
            return;

        AddLanguagesFromHolder(ent);
    }

    private void AddLanguagesFromHolder(Entity<EncryptionKeyHolderComponent> ent)
    {
        if (!TryGetWearer(ent.Owner, out var wearer))
            return;

        if (!TryComp<LanguageComponent>(wearer, out var langComp))
            return;

        foreach (var key in ent.Comp.KeyContainer.ContainedEntities)
        {
            if (!TryComp<LanguageEncryptionKeyComponent>(key, out var langKey))
                continue;

            _language.AddLanguages((wearer.Value, langComp), langKey.Languages, canSpeak: false);
        }
    }

    private void RemoveLanguagesFromHolder(Entity<EncryptionKeyHolderComponent> ent)
    {
        if (!TryGetWearer(ent.Owner, out var wearer))
            return;

        if (!TryComp<LanguageComponent>(wearer, out var langComp))
            return;

        foreach (var key in ent.Comp.KeyContainer.ContainedEntities)
        {
            if (!TryComp<LanguageEncryptionKeyComponent>(key, out var langKey))
                continue;

            foreach (var language in langKey.Languages)
                RemoveKeyLanguage((wearer.Value, langComp), language);
        }
    }

    private bool HasPower(Entity<PowerCellSlotComponent?> ent)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery))
            return false;

        return _battery.GetChargeLevel(battery.Value.AsNullable()) > EmptyChargeLevel;
    }

    private void AddLanguagesFromHeadset(EntityUid headsetUid, EntityUid wearer)
    {
        if (!_container.TryGetContainer(headsetUid, EncryptionKeyHolderComponent.KeyContainerName, out var container))
            return;

        if (!TryComp<LanguageComponent>(wearer, out var langComp))
            return;

        foreach (var key in container.ContainedEntities)
        {
            if (!TryComp<LanguageEncryptionKeyComponent>(key, out var langKey))
                continue;

            _language.AddLanguages((wearer, langComp), langKey.Languages, canSpeak: false);
        }
    }

    private void RemoveLanguagesFromHeadset(EntityUid headsetUid, EntityUid wearer)
    {
        if (!_container.TryGetContainer(headsetUid, EncryptionKeyHolderComponent.KeyContainerName, out var container))
            return;

        if (!TryComp<LanguageComponent>(wearer, out var langComp))
            return;

        foreach (var key in container.ContainedEntities)
        {
            if (!TryComp<LanguageEncryptionKeyComponent>(key, out var langKey))
                continue;

            foreach (var language in langKey.Languages)
            {
                RemoveKeyLanguage((wearer, langComp), language);
            }
        }
    }

    private void RemoveKeyLanguage(Entity<LanguageComponent> ent, ProtoId<LanguagePrototype> language)
    {
        var def = SharedLanguageSystem.GetLanguageDef(ent, language);
        if (def is { CanSpeak: false })
            _language.RemoveLanguage(ent, language);
    }

    private bool TryGetWearer(EntityUid holderUid, out EntityUid? wearer)
    {
        wearer = null;

        if (TryComp<HeadsetComponent>(holderUid, out var headset))
        {
            if (!headset.IsEquipped)
                return false;

            wearer = Transform(holderUid).ParentUid;
            return wearer.Value.IsValid();
        }

        if (!HasComp<LanguageComponent>(holderUid))
            return false;

        wearer = holderUid;
        return true;
    }
}
