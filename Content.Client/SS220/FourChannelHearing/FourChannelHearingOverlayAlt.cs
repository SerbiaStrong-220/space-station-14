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

    private static readonly ProtoId<ShaderPrototype> ShaderProtoId = "FourChannelHearing";
    private readonly ShaderInstance _shader = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly Dictionary<EntityUid, ShaderInstance> _existShaders = [];

    public FourChannelHearingOverlayAlt()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entity.System<TransformSystem>();
        _shader = _prototype.Index(ShaderProtoId).InstanceUnique();
    }

    private const float _waveThikness = 0.7f;
    private const float _waveInterval = 0.2f;
    private const float _waveSpeed = 1.3f;

    private const float _circleWaveRadius = 2.2f;
    private const float _circleWaveDecreaseStart = 1.2f;

    private const float _noDirectedWaveRange = 5f;

    private static readonly Angle _directedWaveAngle = Angle.FromDegrees(15);

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null)
            return;

        if (!_entity.HasComponent<FourChannelHearingComponent>(player.Value))
            return;

        var playerPos = _transform.GetWorldPosition(player.Value);

        var handle = args.WorldHandle;
        var zoom = args.Viewport.Eye?.Zoom ?? Vector2.One;
        var renderScale = args.Viewport.RenderScale;
        var worldToLocalMatrix = args.Viewport.GetWorldToLocalMatrix();

        var query = _entity.EntityQueryEnumerator<FourChannelHearingTargetComponent>();
        while (query.MoveNext(out var uid, out var target))
        {
            if (args.MapId != _transform.GetMapId(uid))
                continue;

            if (!_existShaders.TryGetValue(uid, out var shd))
            {
                shd = _shader.Duplicate();
                _existShaders.Add(uid, shd);
            }

            var targetPos = _transform.GetWorldPosition(uid);

            shd.SetParameter("TargetPos", WorldToLocalPos(targetPos, args.Viewport, worldToLocalMatrix));
            shd.SetParameter("PlayerPos", WorldToLocalPos(playerPos, args.Viewport, worldToLocalMatrix));
            shd.SetParameter("WaveThikness", WorldToLocalLength(_waveThikness, renderScale.X, zoom.X));
            shd.SetParameter("WaveInterval", WorldToLocalLength(_waveInterval, renderScale.X, zoom.X));
            shd.SetParameter("WaveSpeed", _waveSpeed);
            shd.SetParameter("CircleWaveRadius", WorldToLocalLength(_circleWaveRadius, renderScale.X, zoom.X));
            shd.SetParameter("CircleWaveDecreaseStart", WorldToLocalLength(_circleWaveDecreaseStart, renderScale.X, zoom.X));
            shd.SetParameter("NoDirectedWaveRange", WorldToLocalLength(_noDirectedWaveRange, renderScale.X));
            shd.SetParameter("DrawDirectedWave", !args.WorldBounds.Contains(targetPos));
            shd.SetParameter("DirectedWaveAngle", (float)_directedWaveAngle.Theta);
            handle.UseShader(shd);

            var color = target.Color.WithAlpha(0.075f);
            handle.DrawRect(args.WorldBounds, color);
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
