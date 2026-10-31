using System.Linq;
using System.Numerics;
using Content.Client.Inventory;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.SS220.LiquidSplatters;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.LiquidSplatters;

public sealed class LiquidSplattersSystem : SharedLiquidSplattersSystem
{
    private static readonly ProtoId<ShaderPrototype> Shader = "LiquidSplatters";

    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private readonly Dictionary<EntityUid, ShaderInstance> _shaders = new();
    private readonly Dictionary<EntityUid, EquippedLayers> _equippedLayers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LiquidSplattersComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LiquidSplattersComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LiquidSplattersComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<LiquidSplattersComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        SubscribeLocalEvent<LiquidSplattersComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnAfterAutoHandleState(Entity<LiquidSplattersComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        SetShader(ent, ent.Comp.Enabled);
    }

    private void OnStartup(Entity<LiquidSplattersComponent> ent, ref ComponentStartup args)
    {
        TryRestoreEquippedLayers(ent);
        SetShader(ent, ent.Comp.Enabled);
    }

    private void OnShutdown(Entity<LiquidSplattersComponent> ent, ref ComponentShutdown args)
    {
        SetShader(ent, false);
        _equippedLayers.Remove(ent);
    }

    private void OnEquipmentVisualsUpdated(Entity<LiquidSplattersComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        _equippedLayers[ent] = new EquippedLayers(args.Equipee, new HashSet<string>(args.RevealedLayers));
        SetShader(ent, ent.Comp.Enabled);
    }

    private void OnGotUnequipped(Entity<LiquidSplattersComponent> ent, ref GotUnequippedEvent args)
    {
        if (!_equippedLayers.Remove(ent, out var equipped))
            return;

        ClearEquipmentLayerShaders(equipped);
    }

    // This is needed if component is created while the item is already equipped
    // I don't know how it works, but it does
    private void TryRestoreEquippedLayers(EntityUid item)
    {
        if (!_container.TryGetContainingContainer(item, out var container))
            return;

        var wearer = container.Owner;
        if (!TryComp<InventorySlotsComponent>(wearer, out var slots))
            return;

        if (!slots.VisualLayerKeys.TryGetValue(container.ID, out var layerKeys))
            return;

        _equippedLayers[item] = new EquippedLayers(wearer, new HashSet<string>(layerKeys));
    }

    private void SetShader(Entity<LiquidSplattersComponent> ent, bool enabled)
    {
        TryComp(ent, out SpriteComponent? sprite);

        if (!enabled)
        {
            if (sprite != null)
                ClearLayerShaders(sprite);

            if (_equippedLayers.TryGetValue(ent, out var equipped))
                ClearEquipmentLayerShaders(equipped);

            _shaders.Remove(ent);
            return;
        }

        if (!_shaders.TryGetValue(ent, out var shader))
        {
            shader = _proto.Index(Shader).InstanceUnique();
            _shaders[ent] = shader;
        }

        shader.SetParameter("intensity", ent.Comp.Intensity);
        shader.SetParameter("color", ToShaderColor(ent.Comp.Color));
        shader.SetParameter("color_darkness", ent.Comp.ColorDarkness);

        if (sprite != null)
            ApplyLayerShaders(sprite, shader);

        if (_equippedLayers.TryGetValue(ent, out var equippedLayers))
            ApplyEquipmentLayerShaders(equippedLayers, shader);
    }

    private static void ClearLayerShaders(SpriteComponent sprite)
    {
        if (!sprite.AllLayers.Any())
            return;

        if (sprite[0] is not SpriteComponent.Layer layer || layer.ShaderPrototype != Shader.Id)
            return;

        sprite.LayerSetShader(0, null, null);
    }

    private static void ApplyLayerShaders(SpriteComponent sprite, ShaderInstance shader)
    {
        if (!sprite.AllLayers.Any())
            return;

        sprite.LayerSetShader(0, shader, Shader.Id);
    }

    private void ApplyEquipmentLayerShaders(EquippedLayers equipped, ShaderInstance shader)
    {
        if (!TryComp(equipped.Wearer, out SpriteComponent? sprite))
            return;

        foreach (var key in equipped.LayerKeys)
        {
            if (!_sprite.LayerMapTryGet((equipped.Wearer, sprite), key, out var layer, false))
                continue;

            sprite.LayerSetShader(layer, shader, Shader.Id);
        }
    }

    private void ClearEquipmentLayerShaders(EquippedLayers equipped)
    {
        if (!TryComp(equipped.Wearer, out SpriteComponent? sprite))
            return;

        foreach (var key in equipped.LayerKeys)
        {
            if (!_sprite.LayerMapTryGet((equipped.Wearer, sprite), key, out var layerIndex, false))
                continue;

            if (sprite[layerIndex] is not SpriteComponent.Layer layer || layer.ShaderPrototype != Shader.Id)
                continue;

            sprite.LayerSetShader(layerIndex, null, null);
        }
    }

    private static Vector3 ToShaderColor(Color color)
    {
        return new Vector3(color.R, color.G, color.B);
    }

    private sealed record EquippedLayers(EntityUid Wearer, HashSet<string> LayerKeys);
}
