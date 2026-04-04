// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Light.Components;
using Content.Client.Light.EntitySystems;
using Content.Shared.Ghost;
using Content.Shared.Revenant.Components;
using Content.Shared.SS220.DarkReaper;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.SS220.DarkReaper;

public sealed class DarkReaperSystem : SharedDarkReaperSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly LightBehaviorSystem _lightBehavior = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly Color ReaperGhostColor = Color.FromHex("#bbbbff88");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkReaperComponent, AppearanceChangeEvent>(OnAppearanceChange, after: new[] { typeof(GenericVisualizerSystem) });
        SubscribeAllEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        var query = EntityQueryEnumerator<DarkReaperComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (!_appearance.TryGetData(uid, DarkReaperVisual.PhysicalForm, out var data))
                return;

            bool hasGlare = false;
            if (_appearance.TryGetData(uid, DarkReaperVisual.StunEffect, out var glareData))
            {
                if (glareData is bool)
                    hasGlare = (bool)glareData;
            }

            bool ghostCooldown = false;
            if (_appearance.TryGetData(uid, DarkReaperVisual.GhostCooldown, out var ghostCooldownData))
            {
                if (ghostCooldownData is bool)
                    ghostCooldown = (bool)ghostCooldownData;
            }

            if (data is bool isPhysical)
                UpdateAppearance((uid, comp), sprite, isPhysical, hasGlare, ghostCooldown);
        }
    }


    private void UpdateAppearance(Entity<DarkReaperComponent> entity, SpriteComponent sprite, bool isPhysical, bool hasGlare, bool ghostCooldown)
    {
        var controlled = _playerManager.LocalSession?.AttachedEntity;
        var isOwn = controlled == entity.Owner;
        var canSeeOthers = controlled.HasValue &&
                          (HasComp<GhostComponent>(controlled) ||
                           HasComp<DarkReaperComponent>(controlled) ||
                           HasComp<RevenantComponent>(controlled));
        var canSeeGhosted = isOwn || canSeeOthers;

        if (TryComp<LightBehaviourComponent>(entity, out var lightBehaviour))
        {
            if (hasGlare)
                _lightBehavior.StartLightBehaviour((entity, lightBehaviour));
            else
                _lightBehavior.StopLightBehaviour((entity, lightBehaviour));
        }

        Entity<SpriteComponent?> reaperSpriteEntity = (entity.Owner, sprite);
        if (_sprite.LayerMapTryGet(reaperSpriteEntity, DarkReaperVisual.Stage, out var layerIndex, true))
        {
            _sprite.LayerSetVisible(reaperSpriteEntity, layerIndex, (canSeeGhosted || isPhysical) && !ghostCooldown);
            _sprite.LayerSetColor(reaperSpriteEntity, layerIndex, (canSeeGhosted && !isPhysical) ? ReaperGhostColor : Color.White);
        }

        _pointLight.SetEnabled(entity, hasGlare || ghostCooldown);
    }

    private void OnAppearanceChange(Entity<DarkReaperComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(DarkReaperVisual.PhysicalForm, out var data))
            return;

        bool hasGlare = false;
        if (args.AppearanceData.TryGetValue(DarkReaperVisual.StunEffect, out var glareData))
        {
            if (glareData is bool)
                hasGlare = (bool)glareData;
        }

        bool ghostCooldown = false;
        if (args.AppearanceData.TryGetValue(DarkReaperVisual.GhostCooldown, out var ghostCooldownData))
        {
            if (ghostCooldownData is bool)
                ghostCooldown = (bool)ghostCooldownData;
        }

        if (data is bool isPhysical)
            UpdateAppearance(entity, args.Sprite, isPhysical, hasGlare, ghostCooldown);
    }
}
