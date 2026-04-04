// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Entry;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield.Shapes;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ForcefieldCircle : IForcefieldShape
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
    private float _thickness = 0.5f;

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
    public Angle OwnerRotation { get; set; }

    /// <inheritdoc/>
    public bool Dirty { get; set; }

    public Vector2[] InnerPoints { get; private set; } = [];
    public Vector2[] OuterPoints { get; private set; } = [];

    private readonly List<IPhysShape> _cachedPhysShapes = [];
    private readonly List<Vector2> _cachedTrianglesVerts = [];

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

    /// <inheritdoc/>
    public void Refresh()
    {
        RefreshCircles();

        InnerPoints = _innerCircle.GetPoints(Segments);
        OuterPoints = _outerCircle.GetPoints(Segments);

        RefreshPhysShapes();
        RefreshTrianglesVerts();

        Dirty = false;
    }

    private void RefreshCircles()
    {
        var rotationMatrix = Matrix3x2.CreateRotation((float)-OwnerRotation.Opposite().Theta);
        var offset = Vector2.Transform(Offset, rotationMatrix);

        _centralCircle.Radius = Radius;
        _centralCircle.Offset = Offset;

        var radiusOffset = Thickness / 2;

        _innerCircle.Radius = Radius - radiusOffset;
        _innerCircle.Offset = offset;

        _outerCircle.Radius = Radius + radiusOffset;
        _outerCircle.Offset = offset;
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
                throw new Exception($"Failed to generate a {nameof(PolygonShape)} of {nameof(ForcefieldCircle)} for segment: {i}");

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
        return _centralCircle.IsInside(entityPoint);
    }

    /// <inheritdoc/>
    public bool IsOnShape(Vector2 entityPoint)
    {
        return _outerCircle.IsInside(entityPoint) && !_innerCircle.IsInside(entityPoint);
    }

    /// <inheritdoc/>
    public Vector2 GetClosestPoint(Vector2 entityPoint)
    {
        if (IsOnShape(entityPoint))
            return entityPoint;

        var circle = IsInside(entityPoint) ? _innerCircle : _outerCircle;
        return circle.GetClosestPoint(entityPoint);
    }

    /// <inheritdoc/>
    public bool InRange(Vector2 entityPoint, float range)
    {
        if (IsOnShape(entityPoint))
            return true;

        var circle = IsInside(entityPoint) ? _innerCircle : _outerCircle;
        return circle.InRange(entityPoint, range);
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

        private Matrix3x2 EntityToLocal => Matrix3x2.CreateTranslation(-Offset);
        private Matrix3x2 LocalToEntity => Matrix3x2.CreateTranslation(Offset);

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

                points.Add(GetPoint(angle));
            }

            return [.. points];
        }

        public Vector2 GetPoint(Angle angle)
        {
            var x = (float)(Radius * Math.Cos(angle));
            var y = (float)(Radius * Math.Sin(angle));
            return new Vector2(x, y);
        }

        public bool IsInside(Vector2 entityPoint)
        {
            var localPoint = Vector2.Transform(entityPoint, EntityToLocal);
            var posSquared = Math.Pow(localPoint.X, 2) + Math.Pow(localPoint.Y, 2);

            return posSquared <= Radius * Radius;
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
            var checkRange = range + Radius;
            if (distanceToCenter > checkRange)
                return false;

            var closestPoint = GetClosestPoint(entityPoint);
            var distanceToClosest = (entityPoint - closestPoint).Length();
            return distanceToClosest < range;
        }
    }
}

