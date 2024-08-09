// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Ghost;
using Content.Shared.Revenant.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;

namespace Content.Client.SS220.CultYogg;

public sealed class MiHoSystem : SharedMiGoSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private static readonly Color MiGoAstralColor = Color.FromHex("#bbbbff88");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, MiGoAstralAppearanceEvent>(MiGoAstralAppearance);
    }
    private void MiGoAstralAppearance(Entity<MiGoComponent> uid, ref MiGoAstralAppearanceEvent args)
    {

        UpdateAppearance(uid, uid.Comp, uid.Comp.PhysicalForm);
    }
    private void UpdateAppearance(EntityUid uid, MiGoComponent comp, bool isPhysical)
    {
        var controlled = _playerManager.LocalSession?.AttachedEntity;
        var isOwn = controlled == uid;
        var canSeeOthers = controlled.HasValue &&
                          (HasComp<GhostComponent>(controlled) ||
                           HasComp<MiGoComponent>(controlled) ||
                           HasComp<RevenantComponent>(controlled));
        var canSeeGhosted = isOwn || canSeeOthers;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.LayerMapTryGet(MiGoVisual.Base, out var layerIndex))
        {
            sprite.LayerSetVisible(layerIndex, (canSeeGhosted || isPhysical));
            sprite.LayerSetColor(layerIndex, (canSeeGhosted && !isPhysical) ? MiGoAstralColor : Color.White);
        }
    }
}
