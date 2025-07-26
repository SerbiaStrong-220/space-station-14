
using Content.Shared.SS220.Forcefield.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield.Systems;

public sealed class SharedForcefieldSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ForcefieldComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ForcefieldComponent> entity, ref MapInitEvent args)
    {
        if (!TryComp<FixturesComponent>(entity, out var fixtures))
            return;

        var oldFixture = _fixture.GetFixtureOrNull(entity, "fix1", fixtures);
        if (oldFixture is null)
            return;

        var density = oldFixture.Density;
        var mask = oldFixture.CollisionMask;
        var layer = oldFixture.CollisionLayer;

        var parabola = new ForcefieldParabola(4f, 1f, 0.33f, Angle.Zero, new Vector2(0, 2));
        _fixture.DestroyFixture(entity, "fix1", false, manager: fixtures);

        var shapes = parabola.GetShapes();
        for (var i = 0; i < shapes.Count(); i++)
        {
            var shape = shapes.ElementAt(i);
            _fixture.TryCreateFixture(
            entity,
            shape,
            $"shape{i + 1}",
            density: density,
            collisionLayer: layer,
            collisionMask: mask,
            manager: fixtures,
            updates: false
            );
        }

        _fixture.FixtureUpdate(entity, manager: fixtures);
    }

    public static Vector2[] GetParabolaPoints(float length, float height, Angle angle = default, int segments = 64)
    {
        var points = new List<Vector2>();
        var halfLength = length / 2f;
        var startX = -halfLength;
        var endX = halfLength;

        var a = -4f * height / (length * length);
        var rotationMatrix = Matrix3x2.CreateRotation((float)angle.Theta);

        for (var i = 0; i <= segments; i++)
        {
            var t = (float)i / segments;
            var x = MathHelper.Lerp(startX, endX, t);
            var y = a * x * x + height;

            var point = new Vector2(x, y);
            point = Vector2.Transform(point, rotationMatrix);

            points.Add(point);
        }

        return [.. points];
    }
}

public interface IForcefieldFigure
{
    IEnumerable<IPhysShape> GetShapes();
}

public struct ForcefieldParabola : IForcefieldFigure
{
    public Vector2[] FirstArc => _firstArc;
    private Vector2[] _firstArc;

    public Vector2[] SecondArc => _seconsArc;
    private Vector2[] _seconsArc;

    public ForcefieldParabola(float length, float height, float width, Angle angle = default, Vector2 offset = default)
    {
        var vertex = new Vector2(0, height);
        var dir = angle.Opposite().ToWorldVec();

        var halfLength = length / 2f;
        var right = new Vector2(halfLength, 0);
        var vertexToRightNormal = (right - vertex).Normalized();
        var perp = new Vector2(-vertexToRightNormal.Y, vertexToRightNormal.X);
        var lengthChange = perp.X * width;
        var heightChange = (1 - perp.Y) * width / 2;
        var verticalOffset = perp.Y * width / 2;

        var firstLength = length - lengthChange;
        var firstHeight = height - heightChange;
        var firstArc = SharedForcefieldSystem.GetParabolaPoints(firstLength, firstHeight, angle);
        var firstOffset = offset - dir * verticalOffset;
        _firstArc = firstArc.Select(x => x + firstOffset).ToArray();

        var secondLength = length + lengthChange;
        var secondHeight = height + heightChange;
        var secondArc = SharedForcefieldSystem.GetParabolaPoints(secondLength, secondHeight, angle);
        var secondOffset = offset + dir * verticalOffset;
        _seconsArc = secondArc.Select(x => x + secondOffset).ToArray();
    }

    public IEnumerable<IPhysShape> GetShapes()
    {
        var result = new List<IPhysShape>();

        for (var i = 0; i < FirstArc.Length - 1; i++)
        {
            var shape = new PolygonShape();
            shape.Set(new List<Vector2>([FirstArc[i], SecondArc[i], SecondArc[i + 1], FirstArc[i + 1]]));

            result.Add(shape);
        }

        return result;
    }
}
