// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using System.Diagnostics;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield.Shapes;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ForcefieldEllipse : IForcefieldShape
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
    private float _height = 8f;

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
    private int _segments = 64;

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

    private readonly List<IPhysShape> _cachedPhysShapes = [];
    private readonly List<Vector2> _cachedTrianglesVerts = [];

    private readonly Ellipse _innerEllipse = new();
    private readonly Ellipse _centralEllipse = new();
    private readonly Ellipse _outerEllipse = new();

    public ForcefieldEllipse(
        float width,
        float height,
        float thickness,
        Angle angle = default,
        Vector2 offset = default,
        int segments = 64
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

    public ForcefieldEllipse()
    {
        Refresh();
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        RefreshEllipses();

        InnerPoints = _innerEllipse.GetPoints(Segments);
        OuterPoints = _outerEllipse.GetPoints(Segments);

        RefreshPhysShapes();
        RefreshTrianglesVerts();

        Dirty = false;
    }

    private void RefreshEllipses()
    {
        var angle = -OwnerRotation.Opposite() + Angle;

        var rotationMatrix = Matrix3x2.CreateRotation((float)-OwnerRotation.Opposite().Theta);
        var offset = Vector2.Transform(Offset, rotationMatrix);

        _centralEllipse.Width = Width;
        _centralEllipse.Height = Height;
        _centralEllipse.Angle = angle;
        _centralEllipse.Offset = Offset;

        var widthHeightOffset = Thickness;

        _innerEllipse.Width = Width - widthHeightOffset;
        _innerEllipse.Height = Height - widthHeightOffset;
        _innerEllipse.Angle = angle;
        _innerEllipse.Offset = offset;

        _outerEllipse.Width = Width + widthHeightOffset;
        _outerEllipse.Height = Height + widthHeightOffset;
        _outerEllipse.Angle = angle;
        _outerEllipse.Offset = offset;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IPhysShape> GetPhysShapes()
    {
        if (Dirty)
            Refresh();

        return _cachedPhysShapes;
    }

    private void RefreshPhysShapes()
    {
        _cachedPhysShapes.Clear();

        for (var i = 0; i < Segments; i++)
        {
            var shape = new PolygonShape();
            shape.Set(new List<Vector2>([InnerPoints[i], OuterPoints[i], OuterPoints[i + 1], InnerPoints[i + 1]]));

            if (shape.VertexCount <= 0)
                throw new Exception($"Failed to generate a {nameof(PolygonShape)} of {nameof(ForcefieldEllipse)} for segment: {i}");

            _cachedPhysShapes.Add(shape);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<Vector2> GetTrianglesVerts()
    {
        if (Dirty)
            Refresh();

        return _cachedTrianglesVerts;
    }

    private void RefreshTrianglesVerts()
    {
        _cachedTrianglesVerts.Clear();

        for (var i = 0; i < Segments; i++)
        {
            _cachedTrianglesVerts.Add(InnerPoints[i]);
            _cachedTrianglesVerts.Add(OuterPoints[i]);
            _cachedTrianglesVerts.Add(OuterPoints[i + 1]);

            _cachedTrianglesVerts.Add(InnerPoints[i]);
            _cachedTrianglesVerts.Add(InnerPoints[i + 1]);
            _cachedTrianglesVerts.Add(OuterPoints[i + 1]);
        }
    }

    /// <inheritdoc/>
    public bool IsInside(Vector2 entityPoint)
    {
        return _centralEllipse.IsInside(entityPoint);
    }

    /// <inheritdoc/>
    public bool IsOnShape(Vector2 entityPoint)
    {
        return _outerEllipse.IsInside(entityPoint) && !_innerEllipse.IsInside(entityPoint);
    }

    /// <inheritdoc/>
    public Vector2 GetClosestPoint(Vector2 entityPoint)
    {
        if (IsOnShape(entityPoint))
            return entityPoint;

        var ellipse = IsInside(entityPoint) ? _innerEllipse : _outerEllipse;
        return ellipse.GetClosestPoint(entityPoint);
    }

    /// <inheritdoc/>
    public bool InRange(Vector2 entityPoint, float range)
    {
        if (IsOnShape(entityPoint))
            return true;

        var ellipse = IsInside(entityPoint) ? _innerEllipse : _outerEllipse;
        return ellipse.InRange(entityPoint, range);
    }

    [Serializable, NetSerializable]
    private sealed class Ellipse()
    {
        public float Width
        {
            get => _width;
            set
            {
                if (value < 0)
                    throw new ArgumentException("The width cannot be negative.", nameof(Width));

                _width = value;
            }
        }
        private float _width;
        public float Height
        {
            get => _height;
            set
            {
                if (value < 0)
                    throw new ArgumentException("The height cannot be negative.", nameof(Height));

                _height = value;
            }
        }
        private float _height;
        public Angle Angle = default;
        public Vector2 Offset = default;

        private Matrix3x2 EntityToLocal
        {
            get
            {
                var offsetMatrix = Matrix3x2.CreateTranslation(-Offset);
                var rotationMatrix = Matrix3x2.CreateRotation((float)-Angle.Theta);

                return offsetMatrix * rotationMatrix;
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

        public Vector2[] GetPoints(int segments = 64, bool clockwise = true)
        {
            if (segments <= 0)
                throw new ArgumentException("The number of segments must be possitive.", nameof(segments));

            var points = new List<Vector2>();

            var rotationMatrix = Matrix3x2.CreateRotation((float)Angle.Theta);
            var angleStep = 2 * Math.PI / segments;
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * angleStep;
                if (clockwise)
                    angle = -angle;

                var point = GetPoint(angle);
                point = Vector2.Transform(point, rotationMatrix);

                points.Add(point);
            }

            return [.. points];
        }

        public Vector2 GetPoint(Angle angle)
        {
            var x = (float)(Width / 2 * Math.Cos(angle));
            var y = (float)(Height / 2 * Math.Sin(angle));
            return new Vector2(x, y);
        }

        public bool IsInside(Vector2 entityPoint)
        {
            var localPoint = Vector2.Transform(entityPoint, EntityToLocal);

            var a = Width / 2.0;
            var b = Height / 2.0;
            return Math.Pow(localPoint.X / a, 2) + Math.Pow(localPoint.Y / b, 2) <= 1;
        }

        public Vector2 GetClosestPoint(Vector2 entityPoint)
        {
            var localPoint = Vector2.Transform(entityPoint, EntityToLocal);
            var angle = localPoint.ToAngle();

            var closestPoint = GetPoint(angle);
            return Vector2.Transform(closestPoint, LocalToEntity);
        }

        public bool InRange(Vector2 entityPoint, float range)
        {
            var localPoint = Vector2.Transform(entityPoint, EntityToLocal);

            var distanceToCenter = localPoint.Length();
            var checkRange = range + Math.Max(Height, Width);
            if (distanceToCenter > checkRange)
                return false;

            var closestPoint = GetClosestPoint(entityPoint);
            var distanceToClosest = (entityPoint - closestPoint).Length();
            return distanceToClosest < range;
        }
    }
}
