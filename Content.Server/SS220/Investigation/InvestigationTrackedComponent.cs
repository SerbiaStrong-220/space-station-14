// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;

namespace Content.Server.SS220.Investigation;

/// <summary>Added when an entity is first player-controlled and kept for the rest of the round.</summary>
[RegisterComponent]
[Access(typeof(InvestigationRecorderSystem))]
public sealed partial class InvestigationTrackedComponent : Component
{
    public PositionTrack? Track;

    public int? LoadoutHash;

    public HealthSample? Health;

    public bool DirtyLoadout;
}

public readonly record struct SampledPosition(EntityUid? Grid, int Map, Vector2 Local, EntityUid? Container)
{
    public bool DiffersFrom(SampledPosition other, float epsilon)
    {
        if (IsDiscontinuousFrom(other))
            return true;

        return (Local - other.Local).LengthSquared() >= epsilon * epsilon;
    }

    public bool IsDiscontinuousFrom(SampledPosition other)
        => Grid != other.Grid || Container != other.Container || Map != other.Map;
}

public readonly record struct PositionTrack(
    uint WrittenTick,
    SampledPosition WrittenSample,
    uint? PendingTick,
    SampledPosition PendingSample,
    uint LastSampleTick);

public readonly record struct HealthSample(double Damage, string State);
