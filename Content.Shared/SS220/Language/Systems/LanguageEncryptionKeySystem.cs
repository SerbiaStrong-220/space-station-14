// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory.Events;
using Content.Shared.Radio.Components;
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Language.Systems;

public sealed partial class LanguageEncryptionKeySystem : EntitySystem
{
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageEncryptionKeyComponent, EntGotInsertedIntoContainerMessage>(OnKeyInserted);
        SubscribeLocalEvent<LanguageEncryptionKeyComponent, EntGotRemovedFromContainerMessage>(OnKeyRemoved);

        SubscribeLocalEvent<GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<GotUnequippedEvent>(OnUnequipped);
    }

    private void OnKeyInserted(Entity<LanguageEncryptionKeyComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != EncryptionKeyHolderComponent.KeyContainerName)
            return;

        if (!TryGetWearer(args.Container.Owner, out var wearer))
            return;

        if (!TryComp<LanguageComponent>(wearer, out var langComp))
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

    private bool TryGetWearer(EntityUid headsetUid, out EntityUid? wearer)
    {
        wearer = null;

        if (!TryComp<HeadsetComponent>(headsetUid, out var headset) || !headset.IsEquipped)
            return false;

        wearer = Transform(headsetUid).ParentUid;
        return wearer.Value.IsValid();
    }
}

