// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Coordinates;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Stealth.Components;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.SS220.GavelStand;

public sealed class GavelStandSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly UseDelaySystem _delaySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GavelStandComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<GavelStandComponent> entity, ref InteractUsingEvent args)
    {
        var user = args.User;
        var item = args.Used;
        var (gavelStand, gavelStandComponent) = entity;

        var isItemWhitelisted = _whitelistSystem.IsWhitelistPass(entity.Comp.Whitelist, item);
        if (isItemWhitelisted && !_delaySystem.IsDelayed(item))
        {
            _delaySystem.TryResetDelay(item);
            ExclamateAround(gavelStand, user, gavelStandComponent);
            args.Handled = true;
        }
    }

    private void ExclamateTarget(EntityUid target, GavelStandComponent component)
    {
        SpawnAttachedTo(component.Effect, target.ToCoordinates());
    }

    private void ExclamateAround(EntityUid gavelStand, EntityUid owner, GavelStandComponent component)
    {
        StealthComponent? stealth = null;
        _audioSystem.PlayPredicted(component.Sound, gavelStand, owner);
        foreach (var iterator in
            _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(_transform.GetMapCoordinates(gavelStand), component.Distance))
        {
            //Avoid pinging invisible entities
            if (TryComp(iterator, out stealth) && stealth.Enabled)
                continue;

            //We don't want to ping user of whistle
            if (iterator.Owner == owner)
                continue;

            ExclamateTarget(iterator, component);
        }
    }
}
