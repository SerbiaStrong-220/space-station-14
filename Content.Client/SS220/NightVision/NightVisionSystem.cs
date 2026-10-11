using Content.Shared.SS220.NightVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.NightVision;

public sealed partial class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ILightManager _light = default!;
    [Dependency] private SharedMapSystem _maps = default!;

    public override void Initialize()
    {
        base.Initialize();
        var lighting = new NightVisionLightOverlay(this);
        _overlay.AddOverlay(lighting);
        _overlay.AddOverlay(new NightVisionColorOverlay(this, lighting));
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<NightVisionColorOverlay>();
        _overlay.RemoveOverlay<NightVisionLightOverlay>();
        base.Shutdown();
    }

    /// <summary>
    /// Only the local player own eye may use their vision
    /// </summary>
    public NightVisionProfilePrototype? GetProfile(IClydeViewport viewport)
    {
        if (_player.LocalEntity is not { } player ||
            !TryComp<EyeComponent>(player, out var eye) || viewport.Eye != eye.Eye ||
            !eye.Eye.DrawLight || !_light.Enabled || !_light.DrawLighting ||
            !TryComp<MapComponent>(_maps.GetMapOrInvalid(eye.Eye.Position.MapId), out var map) ||
            !map.LightingEnabled)
        {
            return null;
        }

        NightVisionComponent? selected = null;
        var selectedId = int.MaxValue;
        var query = EntityQueryEnumerator<NightVisionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // im really dont want to add this, but if we wear implanter + glasses this muts be
            if (!comp.Enabled || comp.Wearer != player ||
                selected != null && (comp.Priority < selected.Priority ||
                    comp.Priority == selected.Priority && uid.Id >= selectedId))
            {
                continue;
            }

            selected = comp;
            selectedId = uid.Id;
        }

        return selected == null ? null : _prototypes.Index(selected.Profile);
    }
}
