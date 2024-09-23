// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.Speech.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Speech.EntitySystems;

public sealed partial class ClothingSpecialEmotesSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private Dictionary<EntityUid, List<ProtoId<EmotePrototype>>> _temporaryEmotes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingSpecialEmotesComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ClothingSpecialEmotesComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<ClothingSpecialEmotesComponent> entity, ref GotEquippedEvent args)
    {
        if (entity.Comp.Emotes.Count <= 0 ||
            !TryComp<SpeechComponent>(args.Equipee, out var speech))
            return;

        foreach (var newEmote in entity.Comp.Emotes)
        {
            if (speech.AllowedEmotes.Contains(newEmote))
                continue;

            _temporaryEmotes.GetOrNew(entity.Owner).Add(newEmote);
            if (!speech.ClothingEmotes.Contains(newEmote))
                speech.ClothingEmotes.Add(newEmote);
        }
        Dirty(args.Equipee, speech);
    }

    private void OnUnequipped(Entity<ClothingSpecialEmotesComponent> entity, ref GotUnequippedEvent args)
    {
        if (!_temporaryEmotes.TryGetValue(entity.Owner, out var toRemove) ||
            !TryComp<SpeechComponent>(args.Equipee, out var speech))
            return;

        //doesn't remove emotion if equipee has other clothes that give it
        var slotEnumerator = _inventory.GetSlotEnumerator(args.Equipee);
        while (slotEnumerator.NextItem(out var item))
        {
            if (item == entity.Owner ||
                !_temporaryEmotes.TryGetValue(item, out var itemEmotes))
                continue;

            foreach (var itemEmote in itemEmotes)
            {
                toRemove.Remove(itemEmote);
            }
        }

        foreach (var emote in toRemove)
        {
            speech.ClothingEmotes.Remove(emote);
        }

        _temporaryEmotes.Remove(entity.Owner);
        Dirty(args.Equipee, speech);
    }
}
