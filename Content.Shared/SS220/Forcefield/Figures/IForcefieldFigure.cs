// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Physics.Collision.Shapes;
using System.Numerics;

namespace Content.Shared.SS220.Forcefield.Figures;

public interface IForcefieldFigure
{
    Angle OwnerRotation { get; set; }
    bool Dirty { get; set; }
    void Refresh();
    IEnumerable<IPhysShape> GetShapes();
    IEnumerable<Vector2> GetTrianglesVerts();
    bool IsInside(Vector2 point);
}
