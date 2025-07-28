// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield;

public interface IForcefieldFigure
{
    Angle OwnerRotation { get; set; }
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

    public Angle OwnerRotation
    {
        get => _ownerRotation;
        set
        {
            _ownerRotation = value;
            Dirty = true;
        }
    }
    private Angle _ownerRotation = default;
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
        var angle = OwnerRotation + Angle;
        var direction = angle.Opposite().ToWorldVec();

        var vertex = new Vector2(0, Height);
        var right = new Vector2(Width / 2f, 0);
        var rightToVertexNormal = (right - vertex).Normalized();
        var parabolasOffset = new Vector2(-rightToVertexNormal.Y, rightToVertexNormal.X);

        var widthOffset = parabolasOffset.X * Thickness;
        var heightOffset = (1 - parabolasOffset.Y) * Thickness / 2;
        var directionOffset = direction * parabolasOffset.Y * Thickness / 2;

        var innerWidth = Width - widthOffset;
        var innerHeight = Height - heightOffset;
        var innerOffset = Offset - directionOffset;

        var innerPoints = GetParabolaPoints(innerWidth, innerHeight, angle, Segments);
        InnerPoints = [.. innerPoints.Select(x => x + innerOffset)];

        var outerWidth = Width + widthOffset;
        var outerHeight = Height + heightOffset;
        var outerOffset = Offset + directionOffset;

        var outerPoints = GetParabolaPoints(outerWidth, outerHeight, angle, Segments);
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

    public static Vector2[] GetParabolaPoints(float width, float height, Angle angle = default, int segments = 32)
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

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ForcefieldCircle : IForcefieldFigure
{
    [DataField]
    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            Dirty = true;
        }
    }
    private float _radius = 6f;

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
    public int Segments
    {
        get => _segments;
        set
        {
            _segments = value;
            Dirty = true;
        }
    }
    private int _segments = 64;

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

    public Angle OwnerRotation { get; set; }
    public bool Dirty { get; set; }

    public Vector2[] InnerPoints { get; private set; } = [];
    public Vector2[] OuterPoints { get; private set; } = [];

    public void Refresh()
    {
        var radiusOffset = Thickness / 2;

        var innerRadius = Radius - radiusOffset;
        var innerPoints = GetCirclePoints(innerRadius, Segments);
        InnerPoints = [.. innerPoints.Select(x => x + Offset)];

        var outerRadius = Radius + radiusOffset;
        var outerPoints = GetCirclePoints(outerRadius, Segments);
        OuterPoints = [.. outerPoints.Select(x => x + Offset)];

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

    public static Vector2[] GetCirclePoints(float radius, int segments = 64, bool clockwise = true)
    {
        if (segments <= 0)
            throw new ArgumentException("The number of segments cannot be negative.", nameof(segments));
        if (radius < 0)
            throw new ArgumentException("The radius cannot be negative.", nameof(radius));

        var points = new List<Vector2>();

        var angleStep = 2 * Math.PI / segments;
        for (var i = 0; i <= segments; i++)
        {
            var angle = i * angleStep;
            if (clockwise)
                angle = -angle;

            var x = (float)(radius * Math.Cos(angle));
            var y = (float)(radius * Math.Sin(angle));
            points.Add(new Vector2(x, y));
        }

        return [.. points];
    }
}
