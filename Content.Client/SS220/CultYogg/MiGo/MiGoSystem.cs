// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Alerts;
using Content.Client.Movement.Systems;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.MiGo;

public sealed class MiGoSystem : SharedMiGoSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;

    private static readonly Color MiGoAstralColor = Color.FromHex("#bbbbff88");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, MiGoToggleLightEvent>(OnMiGoToggleLightAction);

        SubscribeLocalEvent<MiGoComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<MiGoComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnMiGoToggleLightAction(Entity<MiGoComponent> ent, ref MiGoToggleLightEvent args)
    {
        if (args.Handled)
            return;

        TryComp<PointLightComponent>(ent, out var light);

        if (!TryComp<EyeComponent>(ent, out var eye))
            return;

        if (!eye.DrawLight)
        {
            // normal lighting
            _contentEye.RequestEye(eye.DrawFov, true);
        }
        else if (light != null)
        {
            // personal lighting
            var newState = !light.Enabled;
            _pointLightSystem.SetEnabled(ent.Owner, newState, light);
        }

        args.Handled = true;
    }

    //copypaste from reaper, trying make MiGo transparent without a sprite

    private void OnAppearanceChange(Entity<MiGoComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), MiGoVisual.Base, out var layerIndex, false))
            return;

        _sprite.LayerSetColor((ent, sprite), layerIndex, ent.Comp.IsPhysicalForm ? Color.White : MiGoAstralColor);
    }

    //trying to make alert revenant-like
    private void OnUpdateAlert(Entity<MiGoComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.AstralAlert)
            return;

        var timeLeft = ent.Comp.AlertTime.Int();
        var sprite = args.SpriteViewEnt.Comp;

        _sprite.LayerSetRsiState((args.SpriteViewEnt, sprite), MiGoTimerVisualLayers.Digit1, $"{timeLeft / 10 % 10}");
        _sprite.LayerSetRsiState((args.SpriteViewEnt, sprite), MiGoTimerVisualLayers.Digit2, $"{timeLeft % 10}");
    }
}
