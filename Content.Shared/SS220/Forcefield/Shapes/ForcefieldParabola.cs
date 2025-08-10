// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield.Shapes;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ForcefieldParabola : IForcefieldShape
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
    private float _width = 6f;

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
    private float _thickness = 0.5f;

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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool Dirty { get; set; }

    public Vector2[] InnerPoints { get; private set; } = [];
    public Vector2[] OuterPoints { get; private set; } = [];

    public IPhysShape[] CahcedPhysShapes => _cahcedPhysShapes;
    private IPhysShape[] _cahcedPhysShapes = [];

    public Vector2[] CahcedTrianglesVerts => _cahcedTrianglesVerts;
    private Vector2[] _cahcedTrianglesVerts = [];

    private Parabola _innerParabola = new();
    private Parabola _centralParabola = new();
    private Parabola _outerParabola = new();

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

    /// <inheritdoc/>
    public void Refresh()
    {
        RefreshParabolas();

        InnerPoints = _innerParabola.GetPoints(Segments);
        OuterPoints = _outerParabola.GetPoints(Segments);

        _cahcedPhysShapes = [.. GetPhysShapes()];
        _cahcedTrianglesVerts = [.. GetTrianglesVerts()];

        Dirty = false;
    }

    private void RefreshParabolas()
    {
        var angle = -OwnerRotation.Opposite() + Angle;

        var rotationMatrix = Matrix3x2.CreateRotation((float)-OwnerRotation.Opposite().Theta);
        var offset = Vector2.Transform(Offset, rotationMatrix);

        _centralParabola.Width = Width;
        _centralParabola.Height = Height;
        _centralParabola.Angle = angle;
        _centralParabola.Offset = Offset;

        var direction = angle.Opposite().ToWorldVec();

        var vertex = new Vector2(0, Height);
        var right = new Vector2(Width / 2f, 0);
        var rightToVertexNormal = (right - vertex).Normalized();
        var parabolasOffset = new Vector2(-rightToVertexNormal.Y, rightToVertexNormal.X);

        var widthOffset = parabolasOffset.X * Thickness;
        var heightOffset = (1 - parabolasOffset.Y) * Thickness / 2;
        var directionOffset = direction * parabolasOffset.Y * Thickness / 2;

        _innerParabola.Width = Width - widthOffset;
        _innerParabola.Height = Height - heightOffset;
        _innerParabola.Angle = angle;
        _innerParabola.Offset = offset - directionOffset;

        _outerParabola.Width = Width + widthOffset;
        _outerParabola.Height = Height + heightOffset;
        _outerParabola.Angle = angle;
        _outerParabola.Offset = offset + directionOffset;
    }

    /// <inheritdoc/>
    public IEnumerable<IPhysShape> GetPhysShapes()
    {
        var result = new List<IPhysShape>();

        for (var i = 0; i < Segments; i++)
        {
            var shape = new PolygonShape();
            shape.Set(new List<Vector2>([InnerPoints[i], OuterPoints[i], OuterPoints[i + 1], InnerPoints[i + 1]]));

            if (shape.VertexCount <= 0)
                throw new Exception($"Failed to generate a {nameof(PolygonShape)}");

            result.Add(shape);
        }

        return result;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool IsInside(Vector2 point)
    {
        return _centralParabola.IsInside(point);
    }

    public bool IsOnShape(Vector2 point)
    {
        return _outerParabola.IsInside(point) && !_innerParabola.IsInside(point);
    }

    /// <inheritdoc/>
    public Vector2 GetClosestPoint(Vector2 point)
    {
        if (IsOnShape(point))
            return point;

        var parabola = IsInside(point) ? _innerParabola : _outerParabola;
        return parabola.GetClosestPoint(point);
    }

    public bool InPvS(Vector2 point, float pvsRange)
    {
        var distanceToCenter = point.Length();
        if (distanceToCenter < pvsRange)
            return true;

        var checkRadius = pvsRange + Math.Max(Height, Width / 2);
        if (distanceToCenter > checkRadius)
            return false;

        var closestPoint = GetClosestPoint(point);
        var distanceToClosest = (point - closestPoint).Length();
        return distanceToClosest < pvsRange;
    }

    [Serializable, NetSerializable]
    private sealed class Parabola()
    {
        public float Width
        {
            get => _width;
            set
            {
                if (value < 0)
                    throw new ArgumentException("The width cannot be negative", nameof(Width));

                _width = value;
            }
        }
        private float _width = 0;
        public float Height = 0;
        public Angle Angle = default;
        public Vector2 Offset = default;

        private float A => -Height / (Width / 2 * Width / 2);

        private Matrix3x2 EntityToLocal
        {
            get
            {
                var rotationMatrix = Matrix3x2.CreateRotation((float)-Angle.Theta);
                var offsetMatrix = Matrix3x2.CreateTranslation(-Offset);

                return rotationMatrix * offsetMatrix;
            }
        }

        private Matrix3x2 LocalToEntity
        {
            get
            {
                var rotationMatrix = Matrix3x2.CreateRotation((float)Angle.Theta);
                var offsetMatrix = Matrix3x2.CreateTranslation(Offset);

                return rotationMatrix * offsetMatrix;
            }
        }

        public Vector2[] GetPoints(int segments)
        {
            if (segments <= 0)
                throw new ArgumentException("The number of segments must be possitive.", nameof(segments));

            var points = new List<Vector2>();
            var halfWidth = Width / 2f;
            var startX = -halfWidth;
            var endX = halfWidth;

            var rotationMatrix = Matrix3x2.CreateRotation((float)Angle.Theta);

            for (var i = 0; i <= segments; i++)
            {
                var x = MathHelper.Lerp(startX, endX, (float)i / segments);
                var y = GetY(x);

                var point = new Vector2(x, y);
                point = Vector2.Transform(point, rotationMatrix);
                point += Offset;

                points.Add(point);
            }

            return [.. points];
        }

        public float GetY(float x)
        {
            return A * x * x + Height;
        }

        public (float LeftX, float RightX) GetX(float y)
        {
            var sqrt = MathF.Sqrt(y - Height / A);
            return (-sqrt, sqrt);
        }

        public bool IsInside(Vector2 point)
        {
            point = Vector2.Transform(point, EntityToLocal);

            var parabolaY = GetY(point.X);
            return parabolaY >= point.Y;
        }

        public Vector2 GetClosestPoint(Vector2 point)
        {
            point = Vector2.Transform(point, EntityToLocal);

            var x1 = Math.Clamp(point.X, -Width / 2, Width / 2);
            var y2 = Math.Clamp(point.Y, 0, Height);
            float x2;
            if (point.X > 0)
                x2 = GetX(y2).RightX;
            else
                x2 = GetX(y2).LeftX;

            var centralX = x1 + (x2 - x1) / 2;
            var centralY = GetY(centralX);
            var closestPoint = new Vector2(centralX, centralY);

            return Vector2.Transform(closestPoint, LocalToEntity);
        }
    }
}
