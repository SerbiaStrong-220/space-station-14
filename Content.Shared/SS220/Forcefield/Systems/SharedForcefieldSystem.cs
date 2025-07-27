
using Content.Shared.SS220.Forcefield.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield.Systems;

public abstract class SharedForcefieldSystem : EntitySystem
{
    public void RefreshFigure(Entity<ForcefieldComponent> entity)
    {
        entity.Comp.Figure.Refresh();
        Dirty(entity);

        if (TryComp<FixturesComponent>(entity, out var fixtures))
            RefreshFixtures((entity, entity.Comp, fixtures));
    }

    public virtual void RefreshFixtures(Entity<ForcefieldComponent?, FixturesComponent?> entity)
    {
    }

    public static Vector2[] GetParabolaPoints(float width, float height, Angle angle = default, int segments = 64)
    {
        var points = new List<Vector2>();
        var halfWidth = width / 2f;
        var startX = -halfWidth;
        var endX = halfWidth;

        var a = -4f * height / (width * width);
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
    bool Dirty { get; set; }
    void Refresh();
    IEnumerable<IPhysShape> GetShapes();
    IEnumerable<Vector2> GetTrianglesVerts();
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ForcefieldParabola : IForcefieldFigure
{
    [DataField]
    public float Width
    {
        get => _width;
        set
        {
            _width = value;
            Dirty = true;
        }
    }
    private float _width = 4f;

    [DataField]
    public float Height
    {
        get => _height;
        set
        {
            _height = value;
            Dirty = true;
        }
    }
    private float _height = 0.5f;

    [DataField]
    public float Thickness
    {
        get => _thickness;
        set
        {
            _thickness = value;
            Dirty = true;
        }
    }
    private float _thickness = 0.33f;

    [DataField]
    public Angle Angle
    {
        get => _angle;
        set
        {
            _angle = value;
            Dirty = true;
        }
    }
    private Angle _angle = default;

    [DataField]
    public Vector2 Offset
    {
        get => _offset;
        set
        {
            _offset = value;
            Dirty = true;
        }
    }
    private Vector2 _offset = default;

    [DataField]
    public int Segments
    {
        get => _segments;
        set
        {
            _segments = value;
            Dirty = true;
        }
    }
    private int _segments = 32;

    public bool Dirty { get; set; }
    public Vector2[] InnerPoints { get; private set; } = [];
    public Vector2[] OuterPoints { get; private set; } = [];

    public ForcefieldParabola(
        float width,
        float height,
        float thickness,
        Angle angle = default,
        Vector2 offset = default,
        int segments = 32
    )
    {
        Width = width;
        Height = height;
        Thickness = thickness;
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
        var direction = Angle.Opposite().ToWorldVec();

        var vertex = new Vector2(0, Height);
        var right = new Vector2(Width / 2f, 0);
        var rightToVertexNormal = (right - vertex).Normalized();
        var perp = new Vector2(-rightToVertexNormal.Y, rightToVertexNormal.X);

        var widthOffset = perp.X * Thickness;
        var heightOffset = (1 - perp.Y) * Thickness / 2;
        var directionOffset = direction * perp.Y * Thickness / 2;

        var innerWidth = Width - widthOffset;
        var innerHeight = Height - heightOffset;
        var innerOffset = Offset - directionOffset;

        var innerPoints = SharedForcefieldSystem.GetParabolaPoints(innerWidth, innerHeight, Angle, Segments);
        InnerPoints = [.. innerPoints.Select(x => x + innerOffset)];

        var outerWidth = Width + widthOffset;
        var outerHeight = Height + heightOffset;
        var outerOffset = Offset + directionOffset;

        var outerPoints = SharedForcefieldSystem.GetParabolaPoints(outerWidth, outerHeight, Angle, Segments);
        OuterPoints = [.. outerPoints.Select(x => x + outerOffset)];

        Dirty = false;
    }

    public IEnumerable<IPhysShape> GetShapes()
    {
        var result = new List<IPhysShape>();

        for (var i = 0; i < Segments; i++)
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

        for (var i = 0; i < Segments; i++)
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
