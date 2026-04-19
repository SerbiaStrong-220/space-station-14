// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Vision;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.SS220.Vision;

public sealed class ScotopicVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ScotopicVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ScotopicVisionComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ScotopicVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ScotopicVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnStartup(Entity<ScotopicVisionComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner == _player.LocalEntity)
            SetNightVisionLight(ent, true);
    }

    private void OnRemove(Entity<ScotopicVisionComponent> ent, ref ComponentRemove args)
    {
        SetNightVisionLight(ent, false);
    }

    private void OnPlayerAttached(Entity<ScotopicVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        SetNightVisionLight(ent, true);
    }

    private void OnPlayerDetached(Entity<ScotopicVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveNightVisionLight(ent);
    }

    private void SetNightVisionLight(Entity<ScotopicVisionComponent> ent, bool enabled)
    {
        if (!enabled)
        {
            RemoveNightVisionLight(ent);
            return;
        }

        var visuals = EnsureComp<ScotopicVisionVisualsComponent>(ent);
        visuals.LightEntity ??= SpawnAttachedTo(null, new EntityCoordinates(ent, default));

        var light = EnsureComp<PointLightComponent>(visuals.LightEntity.Value);
        _pointLight.SetMask(ent.Comp.MaskPath, light);

        _pointLight.SetColor(visuals.LightEntity.Value, ent.Comp.Color, light);
        _pointLight.SetRadius(visuals.LightEntity.Value, ent.Comp.Radius, light);
        _pointLight.SetEnergy(visuals.LightEntity.Value, ent.Comp.Energy, light);
        _pointLight.SetSoftness(visuals.LightEntity.Value, ent.Comp.Softness, light);
        _pointLight.SetCastShadows(visuals.LightEntity.Value, false, light);
        _pointLight.SetEnabled(visuals.LightEntity.Value, true, light);
    }

    private void RemoveNightVisionLight(Entity<ScotopicVisionComponent> ent)
    {
        if (!TryComp<ScotopicVisionVisualsComponent>(ent, out var visuals) || visuals.LightEntity == null)
            return;

        Del(visuals.LightEntity.Value);
        visuals.LightEntity = null;
    }
}
