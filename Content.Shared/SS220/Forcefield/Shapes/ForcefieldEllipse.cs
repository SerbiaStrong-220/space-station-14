// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
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
    public IEnumerable<IPhysShape> GetPhysShapes()
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
        return _centralEllipse.IsInside(point);
    }

    /// <inheritdoc/>
    public Vector2? GetClosestPoint(Vector2 point)
    {
        Vector2? result = null;

        var parabolaPoints = IsInside(point) ? InnerPoints : OuterPoints;
        var distance = float.MaxValue;
        foreach (var p in parabolaPoints)
        {
            var dist = (point - p).Length();
            if (dist < distance)
            {
                result = p;
                distance = dist;
            }
        }

        return result;
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

                var x = (float)(Width / 2 * Math.Cos(angle));
                var y = (float)(Height / 2 * Math.Sin(angle));
                var point = new Vector2(x, y);
                point = Vector2.Transform(point, rotationMatrix);

                points.Add(point);
            }

            return [.. points];
        }

        public bool IsInside(Vector2 point)
        {
            var rotationMatrix = Matrix3x2.CreateRotation((float)-Angle.Theta);
            point = Vector2.Transform(point, rotationMatrix);
            point -= Offset;

            var a = Width / 2.0;
            var b = Height / 2.0;
            return Math.Pow(point.X / a, 2) + Math.Pow(point.Y / b, 2) <= 1;
        }
    }
}
