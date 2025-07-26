
using Content.Shared.SS220.Forcefield.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
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
        RefreshFixtures(entity.Owner);
    }

    public void RefreshFigure(Entity<ForcefieldComponent> entity)
    {
        entity.Comp.Figure.Refresh();
        Dirty(entity);
    }

    public void RefreshFixtures(Entity<ForcefieldComponent?, FixturesComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp1) ||
            !Resolve(entity, ref entity.Comp2))
            return;

        RefreshFigure((entity, entity.Comp1));
        var forcefield = entity.Comp1;
        var fixtures = entity.Comp2;

        foreach (var fixture in fixtures.Fixtures)
            _fixture.DestroyFixture(entity, fixture.Key, false, manager: fixtures);

        var shapes = forcefield.Figure.GetShapes();
        for (var i = 0; i < shapes.Count(); i++)
        {
            var shape = shapes.ElementAt(i);
            _fixture.TryCreateFixture(
            entity,
            shape,
            $"shape{i + 1}",
            density: forcefield.Destiny,
            collisionLayer: forcefield.CollisionLayer,
            collisionMask: forcefield.CollisionMask,
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
            var x = MathHelper.Lerp(startX, endX, (float)i / segments);
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
    void Refresh();
    IEnumerable<IPhysShape> GetShapes();
    IEnumerable<Vector2> GetTrianglesVerts();
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ForcefieldParabola : IForcefieldFigure
{
    [DataField]
    public float Length = 4f;

    [DataField]
    public float Height = 0.5f;

    [DataField]
    public float Width = 0.33f;

    [DataField]
    public Angle Angle = default;

    [DataField]
    public Vector2 Offset = default;

    [DataField]
    public int Segments = 64;

    public Vector2[] InnerPoints { get; private set; } = [];
    public Vector2[] OuterPoints { get; private set; } = [];

    public ForcefieldParabola(
        float length,
        float height,
        float width,
        Angle angle = default,
        Vector2 offset = default,
        int segments = 64
    )
    {
        Length = length;
        Height = height;
        Width = width;
        Angle = angle;
        Offset = offset;
        Segments = segments;

        Refresh();
    }

    public ForcefieldParabola()
    {
        Refresh();
    }

    public void Refresh()
    {
        var vertex = new Vector2(0, Height);
        var right = new Vector2(Length / 2f, 0);
        var rightToVertexNormal = (right - vertex).Normalized();
        var perp = new Vector2(-rightToVertexNormal.Y, rightToVertexNormal.X);

        var lengthOffset = perp.X * Width;
        var heightOffset = (1 - perp.Y) * Width / 2;
        var directionOffset = perp.Y * Width / 2;

        var direction = Angle.Opposite().ToWorldVec();

        var innerLength = Length - lengthOffset;
        var innerHeight = Height - heightOffset;
        var innerOffset = Offset - direction * directionOffset;

        var innerPoints = SharedForcefieldSystem.GetParabolaPoints(innerLength, innerHeight, Angle, Segments);
        InnerPoints = [.. innerPoints.Select(x => x + innerOffset)];

        var outerLength = Length + lengthOffset;
        var outerHeight = Height + heightOffset;
        var outerOffset = Offset + direction * directionOffset;

        var outerPoints = SharedForcefieldSystem.GetParabolaPoints(outerLength, outerHeight, Angle, Segments);
        OuterPoints = [.. outerPoints.Select(x => x + outerOffset)];
    }

    public IEnumerable<IPhysShape> GetShapes()
    {
        var result = new List<IPhysShape>();

        for (var i = 0; i < Segments - 1; i++)
        {
            var shape = new PolygonShape();
            shape.Set(new List<Vector2>([InnerPoints[i], OuterPoints[i], OuterPoints[i + 1], InnerPoints[i + 1]]));

            result.Add(shape);
        }

        return result;
    }

    public IEnumerable<Vector2> GetTrianglesVerts()
    {
        var verts = new List<Vector2>();

        for (var i = 0; i < Segments - 1; i++)
        {
            verts.Add(InnerPoints[i]);
            verts.Add(OuterPoints[i]);
            verts.Add(OuterPoints[i + 1]);

            verts.Add(InnerPoints[i]);
            verts.Add(InnerPoints[i + 1]);
            verts.Add(OuterPoints[i + 1]);
        }

        return verts;
    }
}
