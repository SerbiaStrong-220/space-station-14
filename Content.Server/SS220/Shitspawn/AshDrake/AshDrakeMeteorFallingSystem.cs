// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Shitspawn.AshDrake;
using System.Numerics;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeMeteorFallingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AshDrakeMeteorFallingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<TransformComponent>(uid, out var xform))
                continue;
            var cur = _transform.GetWorldPosition(xform);
            var dir = comp.Target - cur;

            if (dir.Length() <= comp.Speed * frameTime)
            {
                _transform.SetWorldPosition(xform, comp.Target);
                RemComp<AshDrakeMeteorFallingComponent>(uid);
                continue;
            }

            _transform.SetWorldPosition(xform, cur + Vector2.Normalize(dir) * comp.Speed * frameTime);
        }
    }
}
