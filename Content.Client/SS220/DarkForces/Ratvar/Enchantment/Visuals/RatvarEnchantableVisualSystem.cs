using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Items.Systems;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.SS220.DarkForces.Ratvar.Enchantment.Visuals;

public sealed class RatvarEnchantableVisualSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RatvarEnchantmentableComponent, GetInhandVisualsEvent>(OnGetVisuals);
        SubscribeLocalEvent<RatvarEnchantmentableComponent, AppearanceChangeEvent>(OnAppearanceChangeEvent);
    }

    private void OnAppearanceChangeEvent(EntityUid uid, RatvarEnchantmentableComponent component,
        AppearanceChangeEvent args)
    {
        _itemSystem.VisualsChanged(uid);
    }

    private void OnGetVisuals(EntityUid uid, RatvarEnchantmentableComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp<ItemComponent>(uid, out var itemComponent) ||
            !TryComp<SpriteComponent>(uid, out var spriteComponent))
            return;

        var layersIndex = spriteComponent.LayerMapReserveBlank("overlay");
        if (!spriteComponent.TryGetLayer(layersIndex, out var overlayLayer))
            return;

        var state = overlayLayer.State.Name;
        if (state == null || !overlayLayer.Visible)
            return;

        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}";
        var overlayKey = defaultKey + $"-{state.Split('-').Last()}";

        if (!TryGetDefaultVisuals(uid, itemComponent, overlayKey, out var layers))
            return;

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{overlayKey}-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }

    private bool TryGetDefaultVisuals(EntityUid uid, ItemComponent item, string defaultKey,
        [NotNullWhen(true)] out List<PrototypeLayerData>? result)
    {
        result = null;

        RSI? rsi = null;

        if (item.RsiPath != null)
            rsi = _resCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / item.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null)
            return false;

        var state = item.HeldPrefix == null ? defaultKey : $"{item.HeldPrefix}-{defaultKey}";

        if (!rsi.TryGetState(state, out _))
            return false;

        var layer = new PrototypeLayerData
        {
            RsiPath = rsi.Path.ToString(),
            State = state,
            MapKeys = new HashSet<string> {state}
        };

        result = new List<PrototypeLayerData> {layer};
        return true;
    }
}
