using Content.Shared.SS220.FourChannelHearing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Client.SS220.FourChannelHearing;

public sealed class FourChannelHearingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly TransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private Dictionary<EntityUid, float> _targetAminProgress = new();

    private float _animSeconds = 1f;

    public FourChannelHearingOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entity.System<TransformSystem>();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var toRem = new HashSet<EntityUid>();
        foreach (var (uid, prog) in _targetAminProgress)
        {
            if (!_entity.EntityExists(uid))
            {
                toRem.Add(uid);
                continue;
            }

            if (!_entity.HasComponent<FourChannelHearingTargetComponent>(uid))
            {
                toRem.Add(uid);
                continue;
            }

            var newProg = prog + args.DeltaSeconds / _animSeconds;
            if (newProg > 1f)
                newProg -= 1f;

            _targetAminProgress[uid] = newProg;
        }

        foreach (var uid in toRem)
            _targetAminProgress.Remove(uid);
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

        var color = Color.LightBlue.WithAlpha(0.125f);
        var handle = args.WorldHandle;
        var query = _entity.EntityQueryEnumerator<FourChannelHearingTargetComponent>();
        while (query.MoveNext(out var uid, out var target))
        {
            const int segmengs = 8;

            if (playerMap != _transform.GetMap(uid))
                continue;

            var targetPos = _transform.GetWorldPosition(uid);

            var toPlayerDir = playerPos - targetPos;
            var arcsAngle = toPlayerDir.ToAngle();
            handle.SetTransform(targetPos, arcsAngle);

            if (!_targetAminProgress.TryGetValue(uid, out var animProg))
            {
                animProg = 0f;
                _targetAminProgress.Add(uid, animProg);
            }

            foreach (var arc in CalculateArcs(toPlayerDir.Length(), animProg))
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, arc.GetTriangulationPoints(segmengs), color);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private IEnumerable<ThickArc> CalculateArcs(float distance, float amimProgress)
    {
        const float startOffset = 1f;
        const float endOffset = 2f;
        const float arcThickness = 0.5f;
        const float emptyThicknes = 1f;
        const double sweepDegrees = 20;

        if (distance <= startOffset)
            yield break;

        if (distance - startOffset <= endOffset)
            yield break;

        amimProgress = Math.Clamp(amimProgress, 0, 1);

        var endPoint = distance - endOffset;

        var betweenStartDist = (arcThickness + emptyThicknes);
        var animOffset = betweenStartDist * amimProgress;

        var sweepAngle = Angle.FromDegrees(sweepDegrees);
        var start = startOffset - betweenStartDist + animOffset;
        while (true)
        {
            var outer = MathF.Min(start + arcThickness, endPoint);
            if (outer > startOffset)
            {
                var inner = MathF.Max(start, startOffset);
                yield return new ThickArc(inner, outer, sweepAngle);
            }

            if (outer >= endPoint)
                yield break;

            start = MathF.Min(start + betweenStartDist, distance);
            if (start >= endPoint)
                yield break;
        }
    }

    private struct ThickArc
    {
        public float InnerRadius { get; private set; }
        public float OuterRadius { get; private set; }
        public Angle SweepAngle { get; private set; }

        private readonly Angle StartAngle => -SweepAngle * 0.5f;

        public ThickArc(float innerRadius, float outerRadius, Angle sweepAngle)
        {
            if (innerRadius > outerRadius)
                (outerRadius, innerRadius) = (innerRadius, outerRadius);

            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            SweepAngle = NormalizeSweep(sweepAngle);
        }

        public readonly Vector2[] GetTriangulationPoints(int segments)
        {
            if (segments <= 0)
                throw new ArgumentException("Количество сегментов должно быть положительным.", nameof(segments));

            var vertices = new Vector2[(segments + 1) * 2];
            var angleStep = SweepAngle / segments;
            var start = StartAngle;

            for (var i = 0; i <= segments; i++)
            {
                var angle = start + i * angleStep;
                vertices[i * 2] = GetCirclePoint(OuterRadius, angle);
                vertices[i * 2 + 1] = GetCirclePoint(InnerRadius, angle);
            }

            return vertices;
        }

        private static Vector2 GetCirclePoint(float radius, Angle angle)
        {
            var x = (float)(radius * Math.Cos(angle));
            var y = (float)(radius * Math.Sin(angle));
            return new Vector2(x, y);
        }

        private static Angle NormalizeSweep(Angle sweep)
        {
            var full = Math.PI * 2;
            var value = sweep.Theta % full;

            if (value < 0) value += full;
            return new Angle(value == 0 ? full : value);
        }
    }
}
