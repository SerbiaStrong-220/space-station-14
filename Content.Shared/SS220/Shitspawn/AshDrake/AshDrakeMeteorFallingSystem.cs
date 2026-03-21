// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using System.Numerics;

namespace Content.Shared.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeMeteorFallingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AshDrakeMeteorFallingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var xform = Transform(uid);
            var cur = _transform.GetWorldPosition(xform);
            var dir = comp.Target - cur;

            if (dir.Length() <= comp.Speed * frameTime)
            {
                _transform.SetWorldPosition(xform, comp.Target);
                if (_net.IsServer)
                    RemComp<AshDrakeMeteorFallingComponent>(uid);
                continue;
            }

            _transform.SetWorldPosition(xform, cur + Vector2.Normalize(dir) * comp.Speed * frameTime);
        }
    }
}
