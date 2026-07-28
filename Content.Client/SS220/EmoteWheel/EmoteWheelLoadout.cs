using System.Linq;
using Content.Shared.Chat.Prototypes;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.EmoteWheel;

/// <summary>
/// The player's chosen emote wheel arrangement: a small number of slots, each holding a fixed number of
/// emotes. Persisted as a client preference, one line per slot with comma separated prototype ids.
/// </summary>
public sealed class EmoteWheelLoadout
{
    /// <summary>
    /// Emotes per slot. Kept low deliberately: sectors have to stay wide enough to flick at without
    /// precision, and wide enough to fit a readable label.
    /// </summary>
    public const int SlotSize = 8;

    /// <summary>
    /// Number of slots. Three is plenty at eight emotes each, and every extra slot is another scroll
    /// step between the player and the emote they want.
    /// </summary>
    public const int SlotCount = 3;

    /// <summary>
    /// Slot contents. Always <see cref="SlotCount"/> lists of exactly <see cref="SlotSize"/> entries;
    /// a null entry is an empty cell.
    /// </summary>
    public readonly List<List<ProtoId<EmotePrototype>?>> Slots;

    private EmoteWheelLoadout(List<List<ProtoId<EmotePrototype>?>> slots)
    {
        Slots = slots;
    }

    /// <summary> An entirely empty loadout. </summary>
    public static EmoteWheelLoadout Empty()
    {
        var slots = new List<List<ProtoId<EmotePrototype>?>>(SlotCount);
        for (var i = 0; i < SlotCount; i++)
        {
            slots.Add(Enumerable.Repeat((ProtoId<EmotePrototype>?) null, SlotSize).ToList());
        }

        return new EmoteWheelLoadout(slots);
    }

    /// <summary>
    /// Reads the arrangement stored for a species. Unknown ids are dropped rather than throwing, so an
    /// emote being renamed or removed costs the player one slot entry instead of a broken wheel.
    /// </summary>
    /// <remarks>
    /// Keyed by species because species is what actually decides which emotes exist for a character - a
    /// Tajaran purrs and a moth flaps, while two humans have identical options. Stored one line per
    /// species as "species=slot|slot|slot", each slot a comma separated list of ids.
    /// </remarks>
    public static EmoteWheelLoadout Load(IConfigurationManager cfg, IPrototypeManager prototypes, string? species)
    {
        var loadout = Empty();

        if (!ReadAll(cfg).TryGetValue(species ?? string.Empty, out var stored))
            return loadout;

        var slots = stored.Split('|');
        for (var slot = 0; slot < SlotCount && slot < slots.Length; slot++)
        {
            var ids = slots[slot].Split(',');
            var written = 0;
            for (var cell = 0; cell < ids.Length && written < SlotSize; cell++)
            {
                var id = ids[cell].Trim();
                if (id.Length == 0 || !prototypes.HasIndex<EmotePrototype>(id))
                    continue;

                loadout.Slots[slot][written++] = id;
            }
        }

        return loadout;
    }

    /// <summary> Writes this arrangement as the given species' wheel, leaving other species alone. </summary>
    public void Save(IConfigurationManager cfg, string? species)
    {
        var all = ReadAll(cfg);
        all[species ?? string.Empty] = string.Join('|',
            Slots.Select(slot => string.Join(',', slot.Where(x => x.HasValue).Select(x => x!.Value.Id))));

        cfg.SetCVar(CCVars220.EmoteWheelLoadout,
            string.Join('\n', all.Select(static x => $"{x.Key}={x.Value}")));
        cfg.SaveToFile();
    }

    private static Dictionary<string, string> ReadAll(IConfigurationManager cfg)
    {
        var result = new Dictionary<string, string>();
        var raw = cfg.GetCVar(CCVars220.EmoteWheelLoadout);

        if (string.IsNullOrWhiteSpace(raw))
            return result;

        foreach (var line in raw.Split('\n'))
        {
            var split = line.IndexOf('=');
            if (split <= 0)
                continue;

            result[line[..split]] = line[(split + 1)..];
        }

        return result;
    }

    /// <summary> True when nothing has been placed, i.e. the player has never configured the wheel. </summary>
    public bool IsEmpty => Slots.All(slot => slot.All(cell => !cell.HasValue));

    /// <summary>
    /// Fills the wheel with the given emotes in order, used when the player has not configured it. Takes
    /// only as many as fit rather than spilling into extra slots, so the default matches the shape the
    /// player will see in the editor.
    /// </summary>
    public static EmoteWheelLoadout Default(IEnumerable<ProtoId<EmotePrototype>> available)
    {
        var loadout = Empty();
        var index = 0;

        foreach (var id in available)
        {
            if (index >= SlotCount * SlotSize)
                break;

            loadout.Slots[index / SlotSize][index % SlotSize] = id;
            index++;
        }

        return loadout;
    }

    /// <summary> Places an emote in a specific cell, clearing it if <paramref name="id"/> is null. </summary>
    public void Set(int slot, int cell, ProtoId<EmotePrototype>? id)
    {
        if (slot < 0 || slot >= SlotCount || cell < 0 || cell >= SlotSize)
            return;

        // Duplicates are allowed on purpose: putting a frequently used emote in the same position on
        // every slot, or twice within one, is a reasonable thing to want.
        Slots[slot][cell] = id;
    }

}
