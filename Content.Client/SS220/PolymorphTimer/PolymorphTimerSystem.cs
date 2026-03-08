// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Alerts;
using Content.Shared.SS220.PolymorphTimer;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.PolymorphTimer;

public sealed class PolymorphTimerSystem : SharedPolymorphTimerSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphTimerComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<PolymorphTimerComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.PolymorphTimerAlert)
            return;

        if (ent.Comp.AlertTime <= 0)
            return;

        var timeLeft = ent.Comp.AlertTime;
        var sprite = args.SpriteViewEnt.Comp;

        _sprite.LayerSetRsiState((args.SpriteViewEnt, sprite), PolymorphTimerVisualLayers.Digit1, $"{timeLeft / 10 % 10}");
        _sprite.LayerSetRsiState((args.SpriteViewEnt, sprite), PolymorphTimerVisualLayers.Digit2, $"{timeLeft % 10}");
    }

}
