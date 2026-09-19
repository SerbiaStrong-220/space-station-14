// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;

namespace Content.Server.SS220.Investigation;

/// <summary>Added to every grid the recorder has written at least one row for.</summary>
[RegisterComponent]
[Access(typeof(InvestigationRecorderSystem))]
public sealed partial class InvestigationGridComponent : Component
{
    public GridPose? Pose;

    public int? BeaconHash;

    public bool SentFullSnapshot;
}

public readonly record struct GridPose(int Map, Vector2 World, float Rotation)
{
    public bool DiffersFrom(GridPose other, float epsilon)
    {
        if (Map != other.Map)
            return true;

        if (MathF.Abs(Rotation - other.Rotation) >= epsilon)
            return true;

        return (World - other.World).LengthSquared() >= epsilon * epsilon;
    }
}
