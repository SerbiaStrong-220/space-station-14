using Content.Client.SS220.TextureFade;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.CultYogg;

public sealed class RaveSystem : SharedRaveSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private readonly EntProtoId _effectPrototype = "CultYoggRaveEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaveComponent, StatusEffectAddedEvent>(OnStatusEffectAdded);
        SubscribeLocalEvent<RaveComponent, ComponentRemove>(OnRemoved);
    }

    private void OnStatusEffectAdded(Entity<RaveComponent> entity, ref StatusEffectAddedEvent args)
    {
        if (args.Key != EffectKey)
            return;
        if (entity.Owner != _playerManager.LocalEntity)
            return;

        var effectEntity = Spawn(_effectPrototype);
        entity.Comp.EffectEntity = effectEntity;
        if (!TryComp<TextureFadeOverlayComponent>(effectEntity, out var overlay))
            return;
        overlay.IsEnabled = true;
    }

    private void OnRemoved(Entity<RaveComponent> entity, ref ComponentRemove args)
    {
        if (entity.Comp.EffectEntity is not { } effectEntity)
            return;
        Del(effectEntity);
    }
}
