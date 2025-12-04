using System.Numerics;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.Vape;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Vape;

public sealed class VapeVisualizerSystem : VisualizerSystem<VapeComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, VapeComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<ItemSlotsComponent>(uid, out var slots))
            return;

        foreach (var part in Enum.GetValues<VapeParts>())
        {
            SpriteSystem.LayerSetVisible(uid, (int)part, false);
        }

        foreach (var (slotName, slot) in slots.Slots)
        {
            if (slot.Item is not { Valid: true } partUid)
                continue;

            if (!HasComp<SpriteComponent>(partUid))
                continue;

            if (!TryComp<VapePartComponent>(partUid, out var part))
                continue;

            if (part.RSIForVape is not SpriteSpecifier.Rsi rsi)
                continue;

            if (!Enum.TryParse<VapeParts>(slotName, true, out var vapePart))
                continue;

            SpriteSystem.LayerSetRsi(uid, (int)vapePart, rsi.RsiPath, rsi.RsiState);
            SpriteSystem.LayerSetOffset(uid, (int)vapePart, part.Offset ?? Vector2.Zero);
            SpriteSystem.LayerSetVisible(uid, (int)vapePart, true);
        }
    }
}
