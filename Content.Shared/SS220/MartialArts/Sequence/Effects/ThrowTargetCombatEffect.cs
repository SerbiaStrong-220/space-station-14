// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Throwing;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

public sealed partial class ThrowTargetCombatEffect : CombatSequenceEffect
{
    [DataField]
    public float Distance = 1f;

    [DataField]
    public float BaseThrowSpeed = 10.0f;

    [DataField]
    public float PushbackRatio = 0f;

    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var throwing = Entity.System<ThrowingSystem>();
        var transform = Entity.System<SharedTransformSystem>();

        if (!Entity.TryGetComponent<TransformComponent>(target, out var targetXform))
            return;

        if (!Entity.TryGetComponent<TransformComponent>(user, out var userXform))
            return;

        var direction = transform.GetMapCoordinates(target, targetXform).Position - transform.GetMapCoordinates(user, userXform).Position;
        var normalized = direction.Normalized();
        var coordinates = targetXform.Coordinates.Offset(normalized * Distance);

        throwing.TryThrow(target, coordinates, BaseThrowSpeed, user, PushbackRatio);
    }
}
