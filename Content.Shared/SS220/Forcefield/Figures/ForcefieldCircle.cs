using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield.Figures;

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
    private int _segments = 64;

    public Angle OwnerRotation { get; set; }
    public bool Dirty { get; set; }

    public Vector2[] InnerPoints { get; private set; } = [];
    public Vector2[] OuterPoints { get; private set; } = [];

    private readonly Circle _innerCircle = new();
    private readonly Circle _centralCircle = new();
    private readonly Circle _outerCircle = new();

    public ForcefieldCircle(float radius, float thickness, Vector2 offset, int segments = 64)
    {
        Radius = radius;
        Thickness = thickness;
        Offset = offset;
        Segments = segments;

        Refresh();
    }

    public ForcefieldCircle()
    {
        Refresh();
    }

    public void Refresh()
    {
        RefreshCircles();

        InnerPoints = _innerCircle.GetPoints(Segments);
        OuterPoints = _outerCircle.GetPoints(Segments);

        Dirty = false;
    }

    private void RefreshCircles()
    {
        _centralCircle.Radius = Radius;
        _centralCircle.Offset = Offset;

        var radiusOffset = Thickness / 2;

        _innerCircle.Radius = Radius - radiusOffset;
        _innerCircle.Offset = Offset;

        _outerCircle.Radius = Radius + radiusOffset;
        _outerCircle.Offset = Offset;
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

    public bool IsInside(Vector2 point)
    {
        return _centralCircle.IsInside(point);
    }

    [Serializable, NetSerializable]
    private sealed class Circle()
    {
        public float Radius
        {
            get => _radius;
            set
            {
                if (value < 0)
                    throw new ArgumentException("The radius cannot be negative.", nameof(Radius));

                _radius = value;
            }
        }
        private float _radius;
        public Vector2 Offset = default;

        public Vector2[] GetPoints(int segments = 64, bool clockwise = true)
        {
            if (segments <= 0)
                throw new ArgumentException("The number of segments cannot be negative.", nameof(segments));

            var points = new List<Vector2>();

            var angleStep = 2 * Math.PI / segments;
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * angleStep;
                if (clockwise)
                    angle = -angle;

                var x = (float)(Radius * Math.Cos(angle));
                var y = (float)(Radius * Math.Sin(angle));
                points.Add(new Vector2(x, y));
            }

            return [.. points];
        }

        public bool IsInside(Vector2 point)
        {
            point -= Offset;
            var posSquared = point.X * point.X + point.Y * point.Y;

            return posSquared <= Radius * Radius;
        }
    }
}

