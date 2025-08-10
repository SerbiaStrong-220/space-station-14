// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Configuration;
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

    IPhysShape[] CahcedPhysShapes { get; }
    Vector2[] CahcedTrianglesVerts { get; }

    /// <summary>
    /// Gets an array consisting of <see cref="IPhysShape"/> to create a hitbox
    /// </summary>
    IEnumerable<IPhysShape> GetPhysShapes();

    /// <summary>
    /// Gets an array consisting of triangles verts for <see cref="DrawPrimitiveTopology.TriangleList"/>
    /// </summary>
    IEnumerable<Vector2> GetTrianglesVerts();

    /// <summary>
    /// Is the <paramref name="point"/> inside the shape
    /// </summary>
    bool IsInside(Vector2 point);

    bool IsOnShape(Vector2 point);

    /// <summary>
    /// Gets the closest point on the shape's boundary to the specified <paramref name="point"/>.
    /// </summary>
    Vector2 GetClosestPoint(Vector2 point);

    bool InPvS(Vector2 point, float pvsRange);
}
