// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.TextureFade;
using Content.Shared.SS220.CultYogg.Rave;
using Content.Shared.SS220.EntityEffects.Events;
using Robust.Client.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.CultYogg.Rave;

public sealed class RaveSystem : SharedRaveSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly EntProtoId _effectPrototype = "CultYoggRaveEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaveComponent, ComponentAdd>(OnAdded);
        SubscribeLocalEvent<RaveComponent, ComponentRemove>(OnRemoved);
        SubscribeLocalEvent<RaveComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<RaveComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RaveComponent, OnChemRemoveHallucinationsEvent>(OnVisionCleansed);
    }

    private void OnAdded(Entity<RaveComponent> entity, ref ComponentAdd args)
    {
        EnsureEffect(entity);
    }

    private void OnVisionCleansed(Entity<RaveComponent> entity, ref OnChemRemoveHallucinationsEvent args)
    {
        RemoveEffect(entity);
    }

    private void OnRemoved(Entity<RaveComponent> entity, ref ComponentRemove args)
    {
        RemoveEffect(entity);
    }

    private void OnPlayerAttached(Entity<RaveComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        EnsureEffect(entity);
    }

    private void OnPlayerDetached(Entity<RaveComponent> entity, ref LocalPlayerDetachedEvent args)
    {
        RemoveEffect(entity);
    }

    private void EnsureEffect(Entity<RaveComponent> entity)
    {
        if (entity.Owner != _playerManager.LocalEntity)
            return;

        var effectEntity = Spawn(_effectPrototype);
        entity.Comp.EffectEntity = effectEntity;

        if (!TryComp<TextureFadeOverlayComponent>(effectEntity, out var overlay))
            return;

        overlay.IsEnabled = true;
    }

    private void RemoveEffect(Entity<RaveComponent> entity)
    {
        if (entity.Comp.EffectEntity is not { } effectEntity
            || !TryComp<TextureFadeOverlayComponent>(effectEntity, out var overlay))
            return;

        overlay.SetUniformProgressionSpeed(0.01f);
        overlay.DeleteAfterFadedOut = true;
    }

    protected override void PlaySound(Entity<RaveComponent> ent)
    {
        _audio.PlayLocal(ent.Comp.RaveSoundCollection, ent, ent);
    }
}
