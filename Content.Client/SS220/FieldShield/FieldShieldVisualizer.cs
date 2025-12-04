// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.SS220.FieldShield;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.SS220.FieldShield;

/// <summary>
/// This handles the display of fire effects on flammable entities.
/// </summary>
public sealed class FieldShieldVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PointLightSystem _lights = default!;

    /// <summary> Afaik minimum radius to at least show light. prototypes qol field </summary>
    private const float MinimalLightRadius = 1.5f;
    /// <summary> Afaik minimum energy to at least show light. prototypes qol field </summary>
    private const float MinimalLightEnergy = 1f;

    private const float MinimalShieldAlpha = 0.4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FieldShieldComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<FieldShieldComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FieldShieldComponent, AfterAutoHandleStateEvent>(OnAfterHandledState);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var fieldShields = EntityQueryEnumerator<FieldShieldComponent, UpdateQueuedFieldShieldComponent>();

        while (fieldShields.MoveNext(out var uid, out var comp, out var updateComp))
        {
            if (_gameTiming.CurTime < comp.RechargeStartTime + comp.RechargeShieldData.RechargeTime)
                continue;

            RemCompDeferred(uid, updateComp);

            if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
                continue;

            UpdateAppearance((uid, comp), sprite, appearance);
        }
    }

    private void OnShutdown(Entity<FieldShieldComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.LightEntity != null)
        {
            QueueDel(entity.Comp.LightEntity.Value);
            entity.Comp.LightEntity = null;
        }

        if (TryComp<SpriteComponent>(entity, out var sprite) &&
            _sprite.LayerMapTryGet((entity, sprite), FieldShieldVisualLayers.Shield, out var layer, false))
        {
            _sprite.RemoveLayer((entity, sprite), layer);
        }
    }

    private void OnComponentInit(Entity<FieldShieldComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite) || !TryComp(entity, out AppearanceComponent? appearance))
            return;

        _sprite.LayerMapReserve((entity, sprite), FieldShieldVisualLayers.Shield);
        _sprite.LayerSetVisible((entity, sprite), FieldShieldVisualLayers.Shield, false);

        sprite.LayerSetShader(FieldShieldVisualLayers.Shield, "unshaded");
    }

    private void OnAfterHandledState(Entity<FieldShieldComponent> entity, ref AfterAutoHandleStateEvent _)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite) || !TryComp(entity, out AppearanceComponent? appearance))
            return;

        if (_sprite.LayerMapTryGet((entity, sprite), FieldShieldVisualLayers.Shield, out var layer, false)
                && entity.Comp.ShieldData.ShieldSprite != null)
            _sprite.LayerSetSprite((entity, sprite), FieldShieldVisualLayers.Shield, entity.Comp.ShieldData.ShieldSprite);

        UpdateAppearance(entity, sprite, appearance);
        EnsureComp<UpdateQueuedFieldShieldComponent>(entity);
    }

    private void UpdateAppearance(Entity<FieldShieldComponent> entity, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_sprite.LayerMapTryGet((entity, sprite), FieldShieldVisualLayers.Shield, out var index, false))
            return;

        var shieldCharge = _gameTiming.CurTime > entity.Comp.RechargeStartTime + entity.Comp.RechargeShieldData.RechargeTime ? entity.Comp.ShieldData.ShieldMaxCharge : entity.Comp.ShieldCharge;
        var shieldChargeRelative = Math.Clamp((float)shieldCharge / entity.Comp.ShieldData.ShieldMaxCharge, 0f, 1f);
        var shieldWork = shieldCharge > 0;

        _sprite.LayerSetVisible((entity, sprite), index, shieldWork);

        var alphaChannelFromShieldCharge = shieldChargeRelative * (1f - MinimalShieldAlpha) + MinimalShieldAlpha;
        _sprite.LayerSetColor((entity, sprite), index, Color.White.WithAlpha(alphaChannelFromShieldCharge));

        if (!shieldWork)
        {
            QueueDel(entity.Comp.LightEntity);
            entity.Comp.LightEntity = null;
            return;
        }

        entity.Comp.LightEntity ??= Spawn(null, new EntityCoordinates(entity, default));
        _transform.SetParent(entity.Comp.LightEntity.Value, entity);

        var light = EnsureComp<PointLightComponent>(entity.Comp.LightEntity.Value);

        _lights.SetColor(entity.Comp.LightEntity.Value, entity.Comp.LightData.Color, light);

        var lightRadius = MinimalLightRadius + entity.Comp.LightData.Radius * shieldChargeRelative;
        var lightEnergy = MinimalLightEnergy + entity.Comp.LightData.Energy * shieldChargeRelative;

        _lights.SetRadius(entity.Comp.LightEntity.Value, lightRadius, light);
        _lights.SetEnergy(entity.Comp.LightEntity.Value, lightEnergy, light);
    }
}

public enum FieldShieldVisualLayers : byte
{
    Shield
}
