// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.SS220.MalfAI;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.SS220.MalfAI;

public sealed partial class MalfAiDoomsdayWaveOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Unshaded = "unshaded";

    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly ShaderInstance _shader;

    private const int TrailRings = 4;
    private const float TrailWidth = 6.5f;
    private const int Ticks = 12;
    private const int ArcSegments = 16;

    private static readonly Color TrailNear = Color.FromHex("#B8F6FF");
    private static readonly Color TrailFar = Color.FromHex("#102A88");
    private static readonly Color RimColor = Color.FromHex("#7AE8FF");
    private static readonly Color InnerColor = Color.FromHex("#2F6CFF");
    private static readonly Color OuterColor = Color.FromHex("#3AA0FF");
    private static readonly Color TickColor = Color.FromHex("#9EE8FF");
    private static readonly Color ArcColor = Color.FromHex("#4CC8FF");

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MalfAiDoomsdayWaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(Unshaded).InstanceUnique();
        ZIndex = 200;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        var viewport = args.WorldAABB.Enlarged(TrailWidth + 1f);
        var query = _ent.EntityQueryEnumerator<MalfAiDoomsdayWaveComponent>();
        while (query.MoveNext(out _, out var wave))
        {
            if (wave.MapId != args.MapId)
                continue;

            var elapsed = (float) (_timing.CurTime - wave.StartTime).TotalSeconds;
            var rings = Math.Max(1, wave.RingCount);
            for (var i = 0; i < rings; i++)
            {
                var local = elapsed - i * wave.RingDelay;
                if (local <= 0f)
                    continue;

                var radius = Math.Min(wave.MaxRadius, local * wave.Speed);
                if (radius <= 0.05f)
                    continue;

                var closestDistSq = (viewport.ClosestPoint(wave.Origin) - wave.Origin).LengthSquared();
                var outer = radius + 0.65f;
                if (closestDistSq > outer * outer)
                    continue;

                var inner = radius - TrailWidth;
                if (inner > 0f)
                {
                    var ox = wave.Origin.X;
                    var oy = wave.Origin.Y;
                    var farX = MathF.Abs(ox - viewport.Left) > MathF.Abs(ox - viewport.Right)
                        ? viewport.Left
                        : viewport.Right;
                    var farY = MathF.Abs(oy - viewport.Bottom) > MathF.Abs(oy - viewport.Top)
                        ? viewport.Bottom
                        : viewport.Top;
                    var far = new Vector2(farX, farY);
                    if ((far - wave.Origin).LengthSquared() < inner * inner)
                        continue;
                }

                DrawRing(handle, wave.Origin, radius, i, viewport);
            }
        }

        handle.UseShader(null);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }

    private void DrawRing(DrawingHandleWorld handle, Vector2 origin, float radius, int ring, Box2 view)
    {
        var time = (float) _timing.RealTime.TotalSeconds;
        var pulse = 0.6f + 0.4f * MathF.Sin(time * 6.2f + ring * 1.7f);
        var flicker = 0.75f + 0.25f * MathF.Sin(time * 17.4f + ring);

        var toCenter = view.Center - origin;
        var dist = toCenter.Length();
        var viewR = view.Size.Length() * 0.5f;
        float start;
        float sweep;
        int segs;
        if (dist <= viewR)
        {
            start = 0f;
            sweep = MathF.Tau;
            segs = Math.Clamp((int) (radius * 2f), 24, 96);
        }
        else
        {
            var mid = MathF.Atan2(toCenter.Y, toCenter.X);
            var half = MathF.Asin(Math.Clamp(viewR / dist, 0f, 1f));
            start = mid - half;
            sweep = half * 2f;
            segs = 32;
        }

        for (var i = 0; i < TrailRings; i++)
        {
            var t = i / (float) (TrailRings - 1);
            var r = radius - t * TrailWidth;
            if (r <= 0.08f)
                continue;

            var falloff = (1f - t) * (1f - t);
            var color = Color.InterpolateBetween(TrailNear, TrailFar, t)
                .WithAlpha(0.2f * falloff * flicker);

            DrawArc(handle, origin, r, color, start, sweep, segs);
        }

        var rimColor = RimColor.WithAlpha(0.72f * pulse);
        var innerColor = InnerColor.WithAlpha(0.5f * pulse);
        var outerColor = OuterColor.WithAlpha(0.3f * pulse);
        DrawArc(handle, origin, radius, rimColor, start, sweep, segs);
        DrawArc(handle, origin, MathF.Max(0.08f, radius - 0.2f), innerColor, start, sweep, segs);
        DrawArc(handle, origin, radius + 0.28f, outerColor, start, sweep, segs);

        var tickColor = TickColor.WithAlpha(0.4f * pulse);
        var spin = time * 0.7f + ring * 0.4f;
        for (var i = 0; i < Ticks; i++)
        {
            var ang = spin + i * (MathF.Tau / Ticks);
            if (!InArc(ang, start, sweep))
                continue;

            var dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
            var inner = origin + dir * MathF.Max(0.05f, radius - 1.2f);
            var outer = origin + dir * (radius + 0.5f * pulse);
            handle.DrawLine(inner, outer, tickColor);
        }

        var arcColor = ArcColor.WithAlpha(0.32f * flicker);
        var arcSpin = -time * 1.15f + ring;
        for (var i = 0; i < ArcSegments; i++)
        {
            if ((i + ring + (int) (time * 9f)) % 5 == 0)
                continue;

            var a0 = arcSpin + i * (MathF.Tau / ArcSegments);
            if (!InArc(a0, start, sweep))
                continue;

            var a1 = arcSpin + (i + 1) * (MathF.Tau / ArcSegments);
            var p0 = origin + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * (radius + 0.65f);
            var p1 = origin + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * (radius + 0.65f);
            handle.DrawLine(p0, p1, arcColor);
        }
    }

    private static void DrawArc(DrawingHandleWorld handle, Vector2 origin, float radius, Color color, float start, float sweep, int segs)
    {
        var step = sweep / segs;
        var prev = origin + new Vector2(MathF.Cos(start), MathF.Sin(start)) * radius;
        for (var i = 1; i <= segs; i++)
        {
            var a = start + i * step;
            var p = origin + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
            handle.DrawLine(prev, p, color);
            prev = p;
        }
    }

    private static bool InArc(float ang, float start, float sweep)
    {
        if (sweep >= MathF.Tau - 0.001f)
            return true;

        var d = (ang - start) % MathF.Tau;
        if (d < 0f)
            d += MathF.Tau;
        return d <= sweep;
    }
}
