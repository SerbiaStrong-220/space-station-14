// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Bodycam;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Bodycam;
public sealed class BodycamVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodycamVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, BodycamVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(BodycamVisualsKey.Key, out var data)
            || data is not BodycamVisuals key
            || args.Sprite == null
            || !args.Sprite.LayerMapTryGet(BodycamVisualsKey.Layer, out int layer)
            || !component.CameraSprites.TryGetValue(key, out var state))
        {
            return;
        }

        args.Sprite.LayerSetState(layer, state);
    }
}
