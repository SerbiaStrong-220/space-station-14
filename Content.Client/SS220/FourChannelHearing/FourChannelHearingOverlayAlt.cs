using Content.Shared.SS220.FourChannelHearing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Client.SS220.FourChannelHearing;

public sealed class FourChannelHearingOverlayAlt : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private readonly TransformSystem _transform;

    private static readonly ProtoId<ShaderPrototype> NoiseShaderProtoId = "FourChannelHearingNoise";
    private readonly ShaderInstance _noiseShader = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private Dictionary<EntityUid, ShaderInstance> _existShaders = new();

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

        _noiseShader.SetParameter("RenderScale", args.Viewport.RenderScale);
        while (query.MoveNext(out var uid, out var target))
        {
            if (playerMap != _transform.GetMap(uid))
                continue;

            if (!_existShaders.TryGetValue(uid, out var shd)){
                shd = _noiseShader.Duplicate();
                _existShaders.Add(uid, shd);
            }

            var targetPos = _transform.GetWorldPosition(uid);

            var toTargetDir = targetPos - playerPos;
            var angle = toTargetDir.ToAngle();

            var color = target.Color.WithAlpha(0.075f);

            var temp = args.Viewport.WorldToLocal(targetPos);
            temp.Y = args.Viewport.Size.Y - temp.Y;
            shd.SetParameter("TargetPos", temp);

            temp = args.Viewport.WorldToLocal(playerPos);
            temp.Y = args.Viewport.Size.Y - temp.Y;
            shd.SetParameter("PlayerPos", temp);

            handle.UseShader(shd);
            handle.SetTransform(playerPos, angle);
            DrawRect();
            // DrawTriangle();

            void DrawRect()
            {
                const float hight = -4f;
                var p1 = new Vector2(0f, -hight);
                var p2 = new Vector2(toTargetDir.Length() + 2.5f, hight);
                var box = Box2.FromTwoPoints(p1, p2);

                handle.DrawRect(box, color);
            }

            void DrawTriangle()
            {
                var p1 = Vector2.Zero;
                var length = toTargetDir.Length() + 1f;
                var p2 = new Vector2(length, 1.5f);
                var p3 = new Vector2(length, -1.5f);

                var points = new List<Vector2>() { p1, p2, p3 };

                var drawVertex = points.Select(x => new DrawVertexUV2D(x, x));
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, Texture.White, drawVertex.ToArray(), color);
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
