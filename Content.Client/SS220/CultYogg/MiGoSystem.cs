// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Ghost;
using Content.Shared.Revenant.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Content.Shared.SS220.CultYogg;

namespace Content.Client.SS220.CultYogg;

public sealed class MiHoSystem : SharedMiGoSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private static readonly Color MiGoAstralColor = Color.FromHex("#bbbbff88");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, AppearanceChangeEvent>(OnAppearanceChange, after: new[] { typeof(GenericVisualizerSystem) });
    }

    private void UpdateAppearance(EntityUid uid, MiGoComponent comp, SpriteComponent sprite, bool isPhysical)
    {
        var controlled = _playerManager.LocalSession?.AttachedEntity;
        var isOwn = controlled == uid;
        var canSeeOthers = controlled.HasValue &&
                          (HasComp<GhostComponent>(controlled) ||
                           HasComp<MiGoComponent>(controlled) ||
                           HasComp<RevenantComponent>(controlled));
        var canSeeGhosted = isOwn || canSeeOthers;

        if (sprite.LayerMapTryGet(MiGoVisual.Base, out var layerIndex))
        {
            sprite.LayerSetVisible(layerIndex, (canSeeGhosted || isPhysical));
            sprite.LayerSetColor(layerIndex, (canSeeGhosted && !isPhysical) ? MiGoAstralColor : Color.White);
        }
    }

    private void OnAppearanceChange(EntityUid uid, MiGoComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateAppearance(uid, component, args.Sprite, component.PhysicalForm);
    }
}
