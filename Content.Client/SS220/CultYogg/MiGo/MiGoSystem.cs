// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Alerts;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.SS220.CultYogg.MiGo;

public sealed class MiGoSystem : SharedMiGoSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly Color MiGoAstralColor = Color.FromHex("#bbbbff88");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<MiGoComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }
    //copypaste from reaper, trying make MiGo transparent without a sprite
    private void OnAppearanceChange(Entity<MiGoComponent> uid, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        if (!sprite.LayerMapTryGet(MiGoVisual.Base, out var layerIndex))
            return;

        sprite.LayerSetColor(layerIndex, uid.Comp.IsPhysicalForm ? Color.White : MiGoAstralColor);
    }
    //trying to make alert revenant-like
    private void OnUpdateAlert(Entity<MiGoComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.AstralAlert)
            return;

        var timeLeft = ent.Comp.AlertTime.Int();
        var sprite = args.SpriteViewEnt.Comp;
        sprite.LayerSetState(MiGoTimerVisualLayers.Digit1, $"{(timeLeft / 10) % 10}");
        sprite.LayerSetState(MiGoTimerVisualLayers.Digit2, $"{(timeLeft % 10)}");
    }
}
