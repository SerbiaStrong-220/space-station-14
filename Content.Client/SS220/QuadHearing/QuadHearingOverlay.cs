using Content.Shared.SS220.QuadHearing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;

namespace Content.Client.SS220.QuadHearing;

public sealed class QuadHearingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly TransformSystem _transform;

    private static readonly ProtoId<ShaderPrototype> ShaderProtoId = "QuadHearing";
    private readonly ShaderInstance _shader = default!;

    private readonly Dictionary<string, TargetsEntry> _targetsEntries = [];
    private readonly List<string> _idQueueRem = [];

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public QuadHearingOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entity.System<TransformSystem>();
        _shader = _prototype.Index(ShaderProtoId).InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        foreach (var (id, entry) in _targetsEntries)
        {
            if (entry.TargetsData.Count <= 0)
            {
                _idQueueRem.Add(id);
                continue;
            }

            entry.FrameUpdate(args);
        }

        foreach (var id in _idQueueRem)
            if (_targetsEntries.Remove(id, out var entry))
                entry.Dispose();

        _idQueueRem.Clear();
    }

    public void RegisterTarget(QuadHearingTargetPrototype proto, EntityCoordinates coords)
    {
        if (!_targetsEntries.TryGetValue(proto.ID, out var entry))
        {
            entry = new(proto, _entity, _timing);
            _targetsEntries.Add(proto.ID, entry);
        }

        if (entry.TargetsData.TryGetValue(coords.EntityId, out var targets))
        {
            var exist = targets.FirstOrDefault(x =>
                (x.Coords.Position - coords.Position).LengthSquared() <= proto.CircleWaveRadius * proto.CircleWaveRadius
                && _timing.CurTime <= x.FadeTime);

            if (exist != null)
                return;
        }

        entry.AddData(new TargetData
        {
            Coords = coords,
            Shader = _shader.Duplicate(),
            FadeTime = _timing.CurTime + proto.FadeDelay,
            DeleteTime = _timing.CurTime + proto.LifeTime
        });
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (!_entity.TryGetComponent<QuadHearingComponent>(player, out var quadHearing))
            return;

        if (!quadHearing.ShowEffect)
            return;

        var handle = args.WorldHandle;
        var zoom = args.Viewport.Eye?.Zoom ?? Vector2.One;
        var renderScale = args.Viewport.RenderScale;
        var worldToLocalMatrix = args.Viewport.GetWorldToLocalMatrix();

        var playerPos = _transform.GetWorldPosition(player);
        var screenPlayerPos = WorldToScreenPos(playerPos, args.Viewport, worldToLocalMatrix);

        foreach (var entry in _targetsEntries.Values)
        {
            var proto = entry.Proto;
            var screenWaveThickness = WorldToScreenLength(proto.WaveThickness, renderScale.X, zoom.X);
            var screenWaveInterval = WorldToScreenLength(proto.WaveInterval, renderScale.X, zoom.X);
            var screenCircleWaveRadius = WorldToScreenLength(proto.CircleWaveRadius, renderScale.X, zoom.X);
            var screenCircleWaveFadeRadius = WorldToScreenLength(proto.CircleWaveFadeRadius, renderScale.X, zoom.X);
            var screenSectorWaveMinDistance = WorldToScreenLength(proto.SectorWaveMinDistance, renderScale.X);

            foreach (var (parent, targets) in entry.TargetsData)
            {
                if (!_entity.EntityExists(parent))
                    continue;

                if (args.MapId != _transform.GetMapId(parent))
                    continue;

                foreach (var data in targets)
                {
                    var worldPos = _transform.ToWorldPosition(data.Coords);
                    var delta = worldPos - playerPos;

                    var color = proto.Color;
                    if (_timing.CurTime > data.FadeTime)
                    {
                        var fadeProg = (_timing.CurTime - data.FadeTime) / (data.DeleteTime - data.FadeTime);
                        color.A *= 1 - Math.Clamp((float)fadeProg, 0, 1);
                    }

                    var shd = data.Shader;
                    shd.SetParameter("TargetPos", WorldToScreenPos(worldPos, args.Viewport, worldToLocalMatrix));
                    shd.SetParameter("PlayerPos", screenPlayerPos);
                    shd.SetParameter("WaveThickness", screenWaveThickness);
                    shd.SetParameter("WaveInterval", screenWaveInterval);
                    shd.SetParameter("WaveSpeed", proto.WaveSpeed);
                    shd.SetParameter("CircleWaveRadius", screenCircleWaveRadius);
                    shd.SetParameter("CircleWaveFadeRadius", screenCircleWaveFadeRadius);
                    shd.SetParameter("DrawSectorWave", !args.WorldBounds.Contains(worldPos));
                    shd.SetParameter("SectorWaveMinDistance", screenSectorWaveMinDistance);
                    shd.SetParameter("SectorWaveAngle", GetSectorWaveAngle(delta.Length(), proto.CircleWaveRadius));
                    shd.SetParameter("NoiseAmplitude", proto.NoiseAmplitude);
                    shd.SetParameter("Color", color);
                    handle.UseShader(shd);

                    handle.DrawRect(args.WorldBounds, color);
                }
            }
        }

        handle.UseShader(null);
    }

    private static float WorldToScreenLength(float length, float renderScale, float zoom = 1)
    {
        return length * renderScale / zoom * EyeManager.PixelsPerMeter;
    }

    private static Vector2 WorldToScreenPos(Vector2 pos, IClydeViewport viewport, Matrix3x2? worldToLocalMatrix = null)
    {
        worldToLocalMatrix ??= viewport.GetWorldToLocalMatrix();
        var localPos = Vector2.Transform(pos, worldToLocalMatrix.Value);
        return new Vector2(localPos.X, viewport.Size.Y - localPos.Y);
    }

    private static float GetSectorWaveAngle(float distanceToCenter, float waveRadius)
    {
        if (distanceToCenter <= waveRadius)
            return MathF.PI * 2f;

        return 2f * MathF.Asin(waveRadius / distanceToCenter);
    }

    private sealed class TargetsEntry(QuadHearingTargetPrototype proto, IEntityManager entityMng, IGameTiming gameTiming) : IDisposable
    {
        private readonly IEntityManager _entity = entityMng;
        private readonly IGameTiming _timing = gameTiming;

        public readonly QuadHearingTargetPrototype Proto = proto;

        private readonly Dictionary<EntityUid, List<TargetData>> _targetsData = [];
        public IReadOnlyDictionary<EntityUid, List<TargetData>> TargetsData => _targetsData;

        private readonly HashSet<TargetData> _datasQueueRem = [];
        private readonly HashSet<EntityUid> _parentsQueueRem = [];

        public void FrameUpdate(FrameEventArgs args)
        {
            foreach (var (parent, targets) in _targetsData)
            {
                if (!_entity.EntityExists(parent) || targets.Count <= 0)
                    _parentsQueueRem.Add(parent);

                foreach (var target in targets)
                    if (_timing.CurTime >= target.DeleteTime)
                        _datasQueueRem.Add(target);
            }

            foreach (var parent in _parentsQueueRem)
                RemoveParent(parent);

            foreach (var data in _datasQueueRem)
                RemoveData(data);

            _datasQueueRem.Clear();
            _parentsQueueRem.Clear();
        }

        public bool RemoveParent(EntityUid parent)
        {
            if (!_targetsData.TryGetValue(parent, out var targets))
                return false;

            foreach (var data in targets)
                data.Dispose();

            targets.Clear();
            return _targetsData.Remove(parent);
        }

        public void AddData(TargetData data)
        {
            var parent = data.Coords.EntityId;
            if (!_targetsData.TryGetValue(parent, out var targets))
            {
                targets = [];
                _targetsData.Add(parent, targets);
            }

            targets.Add(data);
        }

        public bool RemoveData(TargetData data)
        {
            if (!_targetsData.TryGetValue(data.Coords.EntityId, out var targets))
                return false;

            if (!targets.Remove(data))
                return false;

            data.Dispose();
            return true;
        }

        public void Dispose()
        {
            foreach (var data in _targetsData.Values.SelectMany(x => x))
                data.Dispose();

            _targetsData.Clear();
        }
    }

    private sealed class TargetData : IDisposable
    {
        public required EntityCoordinates Coords;
        public required ShaderInstance Shader;
        public TimeSpan FadeTime;
        public TimeSpan DeleteTime;

        public void Dispose()
        {
            Shader.Dispose();
        }
    }
}
