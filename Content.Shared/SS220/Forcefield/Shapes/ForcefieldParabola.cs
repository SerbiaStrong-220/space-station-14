// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Entry;
using Pidgin;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using System.Diagnostics;
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

    private readonly List<IPhysShape> _cachedPhysShapes = [];
    private readonly List<Vector2> _cachedTrianglesVerts = [];

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

        RefreshPhysShapes();
        RefreshTrianglesVerts();

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
                throw new Exception($"Failed to generate a {nameof(PolygonShape)} of {nameof(ForcefieldParabola)} for segment: {i}");

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
        return _centralParabola.IsInside(entityPoint);
    }

    /// <inheritdoc/>
    public bool IsOnShape(Vector2 entityPoint)
    {
        return _outerParabola.IsInside(entityPoint) && !_innerParabola.IsInside(entityPoint);
    }

    /// <inheritdoc/>
    public Vector2 GetClosestPoint(Vector2 entityPoint)
    {
        if (IsOnShape(entityPoint))
            return entityPoint;

        var parabola = IsInside(entityPoint) ? _innerParabola : _outerParabola;
        return parabola.GetClosestPoint(entityPoint);
    }

    /// <inheritdoc/>
    public bool InRange(Vector2 entityPoint, float range)
    {
        if (IsOnShape(entityPoint))
            return true;

        var parabola = IsInside(entityPoint) ? _innerParabola : _outerParabola;
        return parabola.InRange(entityPoint, range);
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

        public bool IsInside(Vector2 entityPoint)
        {
            var localPoint = Vector2.Transform(entityPoint, EntityToLocal);

            var parabolaY = GetY(localPoint.X);
            return parabolaY >= localPoint.Y;
        }

        public Vector2 GetClosestPoint(Vector2 entityPoint)
        {
            var localPoint = Vector2.Transform(entityPoint, EntityToLocal);

            var x1 = Math.Clamp(localPoint.X, -Width / 2, Width / 2);
            var y2 = Math.Clamp(localPoint.Y, 0, Height);
            float x2;
            if (localPoint.X > 0)
                x2 = GetX(y2).RightX;
            else
                x2 = GetX(y2).LeftX;

            var centralX = x1 + (x2 - x1) / 2;
            var centralY = GetY(centralX);
            var closestPoint = new Vector2(centralX, centralY);

            return Vector2.Transform(closestPoint, LocalToEntity);
        }

        public bool InRange(Vector2 entityPoint, float range)
        {
            var localPoint = Vector2.Transform(entityPoint, EntityToLocal);

            var distanceToCenter = localPoint.Length();
            var checkRange = range + Math.Max(Height, Width / 2);
            if (distanceToCenter > checkRange)
                return false;

            var closestPoint = GetClosestPoint(entityPoint);
            var distanceToClosest = (entityPoint - closestPoint).Length();
            return distanceToClosest < range;
        }
    }
}
