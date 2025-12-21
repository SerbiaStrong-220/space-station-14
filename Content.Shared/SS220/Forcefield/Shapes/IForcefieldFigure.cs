// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Physics.Collision.Shapes;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield.Shapes;

public interface IForcefieldShape
{
    /// <summary>
    /// Rotation of the force field owner's entity
    /// </summary>
    Angle OwnerRotation { get; set; }

    /// <summary>
    /// Is it necessary to refresh the force field shape
    /// </summary>
    bool Dirty { get; set; }
    void Refresh();

    /// <summary>
    /// Gets an array consisting of <see cref="IPhysShape"/> to create a hitbox
    /// </summary>
    IReadOnlyList<IPhysShape> GetPhysShapes();

    /// <summary>
    /// Gets an array consisting of triangles verts for <see cref="DrawPrimitiveTopology.TriangleList"/>
    /// </summary>
    IReadOnlyList<Vector2> GetTrianglesVerts();

    /// <summary>
    /// Is the <paramref name="entityPoint"/> inside the shape area
    /// </summary>
    bool IsInside(Vector2 entityPoint);

    /// <summary>
    /// Is the <paramref name="entityPoint"/> on the shape
    /// </summary>
    bool IsOnShape(Vector2 entityPoint);

    /// <summary>
    /// Gets the closest point on the shape's boundary to the specified <paramref name="entityPoint"/>.
    /// </summary>
    Vector2 GetClosestPoint(Vector2 entityPoint);

    /// <summary>
    /// Is the shape within the <paramref name="range"/> of a <paramref name="entityPoint"/>.
    /// </summary>
    bool InRange(Vector2 entityPoint, float range);
}
