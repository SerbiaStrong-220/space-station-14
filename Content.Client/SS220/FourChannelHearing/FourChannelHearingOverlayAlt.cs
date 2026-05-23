using Content.Shared.SS220.FourChannelHearing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.SS220.FourChannelHearing;

public sealed class FourChannelHearingOverlayAlt : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly TransformSystem _transform;

    private static readonly ProtoId<ShaderPrototype> NoiseShaderProtoId = "FourChannelHearingNoise";
    private readonly ShaderInstance _noiseShader = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public FourChannelHearingOverlayAlt()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entity.System<TransformSystem>();
        _noiseShader = _prototype.Index(NoiseShaderProtoId).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null)
            return;

        if (!_entity.HasComponent<FourChannelHearingComponent>(player.Value))
            return;

        var playerMap = _transform.GetMap(player.Value);
        var playerPos = _transform.GetWorldPosition(player.Value);

        var handle = args.WorldHandle;
        var query = _entity.EntityQueryEnumerator<FourChannelHearingTargetComponent>();

        _noiseShader.SetParameter("u_time", (float)10);
        handle.UseShader(_noiseShader);
        while (query.MoveNext(out var uid, out var target))
        {
            if (playerMap != _transform.GetMap(uid))
                continue;

            var targetPos = _transform.GetWorldPosition(uid);

            var toPlayerDir = playerPos - targetPos;
            var angle = toPlayerDir.ToAngle();

            var color = target.Color.WithAlpha(0.075f);

            var p1 = new Vector2(0f, -0.5f);
            var p2 = new Vector2(toPlayerDir.Length(), 0.5f);
            var box = Box2.FromTwoPoints(p1, p2);

            handle.SetTransform(targetPos, angle);
            handle.DrawRect(box, color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
