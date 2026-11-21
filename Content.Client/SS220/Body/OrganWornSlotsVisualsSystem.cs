// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.DisplacementMap;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.DisplacementMap;
using Content.Shared.Inventory;
using Content.Shared.SS220.Body;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Body;

/// <summary>
/// Draws clothes left on some of the organs they are worn on through the mask of the missing organ, e.g. a single shoe on the remaining foot.
/// </summary>
public sealed partial class OrganWornSlotsVisualsSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private void OnEquipmentVisualsUpdated(EquipmentVisualsUpdatedEvent args)
    {
        if (!TryComp<OrganWornSlotsComponent>(args.Equipee, out var worn)
            || !TryComp<SpriteComponent>(args.Equipee, out var sprite)
            || !_inventory.TryGetSlot(args.Equipee, args.Slot, out var slot)
            || !TryGetMissingOrgan((args.Equipee, worn), slot.SlotFlags, out var category)
            || !worn.Masks.TryGetValue(category, out var mask))
            return;

        var equipee = new Entity<SpriteComponent>(args.Equipee, sprite);

        // the revealed layers also hold the displacement layers of the clothes, they are replaced, not masked
        var clothingLayers = new List<(string Key, bool Displaced)>();
        foreach (var key in args.RevealedLayers)
        {
            if (!_sprite.LayerMapTryGet(equipee.AsNullable(), key, out var index, false)
                || sprite[index] is not SpriteComponent.Layer { CopyToShaderParameters: null } layer)
                continue;

            var displaced = layer.ShaderPrototype is { } shader
                && (shader.Id == mask.ShaderOverride || shader.Id == mask.ShaderOverrideUnshaded);
            clothingLayers.Add((key, displaced));
        }

        foreach (var (key, displaced) in clothingLayers)
        {
            var layerMask = !displaced && worn.UndisplacedMasks.TryGetValue(category, out var undisplacedMask)
                ? undisplacedMask
                : mask;

            ApplyMask(equipee, key, layerMask, args.RevealedLayers);
        }
    }

    private bool TryGetMissingOrgan(Entity<OrganWornSlotsComponent> ent, SlotFlags slotFlags, out ProtoId<OrganCategoryPrototype> category)
    {
        foreach (var (organCategory, slots) in ent.Comp.Slots)
        {
            if ((slots & slotFlags) == SlotFlags.NONE || _body.HasOrganCategory(ent.Owner, organCategory))
                continue;

            category = organCategory;
            return true;
        }

        category = default;
        return false;
    }

    // a layer holds only one displacement map, so the mask replaces the species one (species masks include their displacement)
    private void ApplyMask(Entity<SpriteComponent> equipee, string key, DisplacementData mask, HashSet<string> revealedLayers)
    {
        _displacement.EnsureDisplacementIsNotOnSprite(equipee, key);
        if (!_sprite.LayerMapTryGet(equipee.AsNullable(), key, out var index, false))
            return;

        if (_displacement.TryAddDisplacement(mask, equipee, index, key, out var displacementKey))
            revealedLayers.Add(displacementKey);
    }
}
