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

    private Dictionary<EntityUid, ShaderInstance> _existShaders = new();

    public FourChannelHearingOverlayAlt()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entity.System<TransformSystem>();
        _noiseShader = _prototype.Index(NoiseShaderProtoId).InstanceUnique();
    }

    const float _waveThikness = 0.7f;
    const float _waveInterval = 0.2f;

    const float _circleWaveRadius = 2.2f;
    const float _circleWaveDecreaseStart = 1.2f;

    const float _noDirectedWaveRange = 7f;

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

        var zoom = args.Viewport.Eye?.Zoom ?? Vector2.One;
        var renderScale = args.Viewport.RenderScale;
        var worldToLocalMatrix = args.Viewport.GetWorldToLocalMatrix();
        while (query.MoveNext(out var uid, out var target))
        {
            if (playerMap != _transform.GetMap(uid))
                continue;

            if (!_existShaders.TryGetValue(uid, out var shd)){
                shd = _noiseShader.Duplicate();
                _existShaders.Add(uid, shd);
            }

            var targetPos = _transform.GetWorldPosition(uid);

            shd.SetParameter("TargetPos", WorldToLocalPos(targetPos, args.Viewport, worldToLocalMatrix));
            shd.SetParameter("PlayerPos", WorldToLocalPos(playerPos, args.Viewport, worldToLocalMatrix));
            shd.SetParameter("WaveThikness", WorldToLocalLength(_waveThikness, renderScale.X, zoom.X));
            shd.SetParameter("WaveInterval", WorldToLocalLength(_waveInterval, renderScale.X, zoom.X));
            shd.SetParameter("CircleWaveRadius", WorldToLocalLength(_circleWaveRadius, renderScale.X, zoom.X));
            shd.SetParameter("CircleWaveDecreaseStart", WorldToLocalLength(_circleWaveDecreaseStart, renderScale.X, zoom.X));
            shd.SetParameter("NoDirectedWaveRange", WorldToLocalLength(_noDirectedWaveRange, renderScale.X));
            handle.UseShader(shd);

            var toTargetDir = targetPos - playerPos;
            var angle = toTargetDir.ToAngle();
            var color = target.Color.WithAlpha(0.075f);

            handle.SetTransform(playerPos, angle);
            DrawRect();

            void DrawRect()
            {
                const float hight = -4f;
                var p1 = new Vector2(0f, -hight);
                var p2 = new Vector2(toTargetDir.Length() + 2.5f, hight);
                var box = Box2.FromTwoPoints(p1, p2);

                handle.DrawRect(box, color);
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private static float WorldToLocalLength(float length, float renderScale, float zoom = 1)
    {
        return length * renderScale / zoom * EyeManager.PixelsPerMeter;
    }

    private static Vector2 WorldToLocalPos(Vector2 pos, IClydeViewport viewport, Matrix3x2? worldToLocalMatrix = null)
    {
        worldToLocalMatrix ??= viewport.GetWorldToLocalMatrix();
        var localPos = Vector2.Transform(pos, worldToLocalMatrix.Value);
        return new Vector2(localPos.X, viewport.Size.Y - localPos.Y);
    }
}
