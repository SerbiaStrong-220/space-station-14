using Content.Shared._Sunrise.Clothing.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Shared._Sunrise.Clothing.EntitySystems;

public sealed class EmitSoundOnWearerMoveSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<InputMoverComponent> _inputMover;

    public const float MinDistanceSprinting = 1.5f;
    public const float MinDistanceWaling = 2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _inputMover = _entMan.GetEntityQuery<InputMoverComponent>();

        SubscribeLocalEvent<EmitSoundOnWearerMoveComponent, GotEquippedEvent>(OnEquipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmitSoundOnWearerMoveComponent, TransformComponent, ClothingComponent>();
        while (query.MoveNext(out var uid, out var emitSoundOnMoveComponent, out var xform, out var clothing))
        {
            if (xform.GridUid == null)
                continue;

            if (emitSoundOnMoveComponent.RequiresGravity && _gravity.IsWeightless(uid))
                continue;

            var wearer = xform.ParentUid;
            var worn = wearer.Valid &&
                       clothing.InSlot != null &&
                       emitSoundOnMoveComponent.IsValidSlot;

            var coords = Transform(wearer).Coordinates;
            var dist = (worn && _inputMover.TryGetComponent(wearer, out var mover) && mover.Sprinting)
                ? MinDistanceSprinting
                : MinDistanceWaling;
            if (!coords.TryDistance(EntityManager, emitSoundOnMoveComponent.LastPosition, out var distance) ||
                distance > dist)
                emitSoundOnMoveComponent.SoundDistance = dist;
            else
                emitSoundOnMoveComponent.SoundDistance += distance;

            emitSoundOnMoveComponent.LastPosition = coords;
            if (emitSoundOnMoveComponent.SoundDistance < dist)
                continue;
            emitSoundOnMoveComponent.SoundDistance -= dist;

            var sound = emitSoundOnMoveComponent.Sound;
            _audio.PlayPredicted(
                sound,
                uid,
                uid,
                sound.Params.WithVolume(sound.Params.Volume).WithVariation(sound.Params.Variation ?? 0f));
            Dirty(uid, emitSoundOnMoveComponent);
        }
    }

    private void OnEquipped(EntityUid uid, EmitSoundOnWearerMoveComponent component, GotEquippedEvent args)
    {
        component.IsValidSlot = !args.SlotFlags.HasFlag(SlotFlags.POCKET);
    }
}