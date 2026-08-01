// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pinpointer;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.Investigation;

public sealed partial class InvestigationRecorderSystem
{
    /// <inheritdoc/>
    public bool TryGetPosition(EntityUid uid, out SampledPosition position)
    {
        position = default;

        if (!_xformQuery.TryComp(uid, out var xform))
            return false;

        EntityUid? container = null;
        if (_container.TryGetContainingContainer((uid, xform, null), out var containing))
            container = containing.Owner;

        var grid = xform.GridUid;
        var worldPos = _transform.GetWorldPosition(uid);
        var local = grid is { } gridUid
            ? Vector2.Transform(worldPos, _transform.GetInvWorldMatrix(gridUid))
            : worldPos;

        position = new SampledPosition(grid, (int) xform.MapID, local, container);
        return true;
    }

    private void SamplePositions()
    {
        _gridMatrices.Clear();

        var tick = _timing.CurTick.Value;
        var query = EntityQueryEnumerator<InvestigationTrackedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tracked, out var xform))
        {
            EntityUid? container = null;
            if (_container.TryGetContainingContainer((uid, xform, null), out var containing))
                container = containing.Owner;

            var grid = xform.GridUid;
            var worldPos = _transform.GetWorldPosition(uid);

            var local = grid is { } gridUid
                ? Vector2.Transform(worldPos, GetInvGridMatrix(gridUid))
                : worldPos;

            RecordPosition((uid, tracked), tick, new SampledPosition(grid, (int) xform.MapID, local, container));
            SampleHealth((uid, tracked));
        }
    }

    /// <remarks>
    ///    Two filters:
    ///     An epsilon that drops unmoved entities
    ///     Dead reckoning. Only direction changes become rows.
    /// </remarks>
    public void RecordPosition(Entity<InvestigationTrackedComponent> ent, uint tick, SampledPosition observed)
    {
        var uid = ent.Owner;
        var tracked = ent.Comp;

        _recorder.UpdateKnownPosition(uid, observed);

        if (tracked.Track is not { } track)
        {
            _recorder.WritePositionRow(uid, tick, observed);
            tracked.Track = new PositionTrack(tick, observed, null, default, tick);
            return;
        }

        if (observed.IsDiscontinuousFrom(track.WrittenSample))
        {
            FlushPending(uid, ref track);
            _recorder.WritePositionRow(uid, tick, observed);
            tracked.Track = new PositionTrack(tick, observed, null, default, tick);
            return;
        }

        if (track.PendingTick is not { } pendingTick)
        {
            if (!observed.DiffersFrom(track.WrittenSample, _positionEpsilon))
            {
                tracked.Track = track with { LastSampleTick = tick };
                return;
            }

            // Re-emit the held position to close a stationary stretch, or interpolation drifts across it.
            if (track.LastSampleTick > track.WrittenTick)
            {
                _recorder.WritePositionRow(uid, track.LastSampleTick, track.WrittenSample);
                track = track with { WrittenTick = track.LastSampleTick };
            }

            tracked.Track = track with { PendingTick = tick, PendingSample = observed, LastSampleTick = tick };
            return;
        }

        var span = tick - track.WrittenTick;
        var fraction = span == 0 ? 0f : (pendingTick - track.WrittenTick) / (float) span;
        var predicted = Vector2.Lerp(track.WrittenSample.Local, observed.Local, fraction);

        if ((track.PendingSample.Local - predicted).LengthSquared() <= _positionEpsilon * _positionEpsilon)
        {
            tracked.Track = track with { PendingTick = tick, PendingSample = observed, LastSampleTick = tick };
            return;
        }

        _recorder.WritePositionRow(uid, pendingTick, track.PendingSample);
        tracked.Track = new PositionTrack(pendingTick, track.PendingSample, tick, observed, tick);
    }

    private void FlushPending(EntityUid uid, ref PositionTrack track)
    {
        if (track.PendingTick is not { } pendingTick)
            return;

        _recorder.WritePositionRow(uid, pendingTick, track.PendingSample);
        track = new PositionTrack(pendingTick, track.PendingSample, null, default, track.LastSampleTick);
    }

    /// <summary>Writes every held-back sample. Called at round end so no last movement is dropped.</summary>
    public void FlushPendingPositions()
    {
        var query = EntityQueryEnumerator<InvestigationTrackedComponent>();
        while (query.MoveNext(out var uid, out var tracked))
        {
            if (tracked.Track is not { } track)
                continue;

            FlushPending(uid, ref track);
            tracked.Track = track;
        }
    }

    private Matrix3x2 GetInvGridMatrix(EntityUid grid)
    {
        if (_gridMatrices.TryGetValue(grid, out var cached))
            return cached;

        var matrix = _transform.GetInvWorldMatrix(grid);
        _gridMatrices[grid] = matrix;
        return matrix;
    }

    private void SampleHealth(Entity<InvestigationTrackedComponent> ent)
    {
        var uid = ent.Owner;
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        // GetTotalDamage is obsolete; a health bar is exactly the one-number reduction it warns about.
#pragma warning disable CS0618
        var total = (float) _damageable.GetTotalDamage((uid, damageable));
#pragma warning restore CS0618

        // Copied to a local: MobStateComponent is [Access]-restricted, and a direct call reads as execute access.
        var state = "Unknown";
        if (TryComp<MobStateComponent>(uid, out var mobState))
        {
            var current = mobState.CurrentState;
            state = current.ToString();
        }

        float? crit = _thresholds.TryGetIncapThreshold(uid, out var incap) ? (float) incap.Value : null;
        float? dead = _thresholds.TryGetDeadThreshold(uid, out var deadAt) ? (float) deadAt.Value : null;

        var sample = new HealthSample(Math.Round(total, 2), state);
        if (ent.Comp.Health == sample)
            return;

        ent.Comp.Health = sample;
        _recorder.WriteHealthRow(uid, sample, crit, dead);
    }

    private void SampleGridPoses()
    {
        var query = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            var (world, rotation) = _transform.GetWorldPositionRotation(xform);
            var pose = new GridPose((int) xform.MapID, world, (float) rotation.Theta);

            var grid = EnsureComp<InvestigationGridComponent>(uid);
            if (grid.Pose is { } previous && !pose.DiffersFrom(previous, GridPoseEpsilon))
                continue;

            grid.Pose = pose;

            var mapName = _metaQuery.TryComp(xform.MapUid, out var mapMeta) ? mapMeta.EntityName : null;
            _recorder.WriteGridPose(uid, pose, mapName);
        }
    }

    /// <summary>
    ///     Floor footprint for grids with no <see cref="NavMapComponent"/> — shuttles, salvage wrecks, debris.
    ///     Emitted once.
    /// </summary>
    private void SampleGridFootprints()
    {
        var query = EntityQueryEnumerator<MapGridComponent>();
        while (query.MoveNext(out var uid, out var mapGrid))
        {
            if (_navMapQuery.HasComp(uid))
                continue;

            var grid = EnsureComp<InvestigationGridComponent>(uid);
            if (grid.SentFullSnapshot)
                continue;

            grid.SentFullSnapshot = true;

            var chunks = new Dictionary<Vector2i, int[]>();
            foreach (var tile in _map.GetAllTiles(uid, mapGrid))
            {
                var indices = tile.GridIndices;
                var origin = SharedMapSystem.GetChunkIndices(indices, SharedNavMapSystem.ChunkSize);

                if (!chunks.TryGetValue(origin, out var tiles))
                    chunks[origin] = tiles = new int[SharedNavMapSystem.ArraySize];

                var relative = SharedMapSystem.GetChunkRelative(indices, SharedNavMapSystem.ChunkSize);
                tiles[SharedNavMapSystem.GetTileIndex(relative)] |= SharedNavMapSystem.FloorMask;
            }

            foreach (var (origin, tiles) in chunks)
            {
                _recorder.WriteNavMapChunk(uid, origin, tiles);
            }
        }
    }

    private void SampleNavMap()
    {
        var sweepTick = _lastNavMapTick;
        _lastNavMapTick = _timing.CurTick;

        var query = EntityQueryEnumerator<NavMapComponent, MapGridComponent>();
        while (query.MoveNext(out var uid, out var navMap, out _))
        {
            var grid = EnsureComp<InvestigationGridComponent>(uid);

            foreach (var (origin, chunk) in navMap.Chunks)
            {
                if (grid.SentFullSnapshot && chunk.LastUpdate < sweepTick)
                    continue;

                _recorder.WriteNavMapChunk(uid, origin, chunk.TileData);
            }

            grid.SentFullSnapshot = true;

            var beacons = ComputeBeaconHash(navMap);
            if (grid.BeaconHash != beacons)
            {
                _recorder.WriteNavMapBeacons(uid, navMap.Beacons.Values.Select(object (beacon) => new
                {
                    name = beacon.Text,
                    x = Math.Round(beacon.Position.X, 2),
                    y = Math.Round(beacon.Position.Y, 2),
                }));

                grid.BeaconHash = beacons;
            }
        }
    }

    private static int ComputeBeaconHash(NavMapComponent navMap)
    {
        var combined = navMap.Beacons.Count;

        foreach (var beacon in navMap.Beacons.Values)
        {
            combined ^= HashCode.Combine(beacon.Text, beacon.Position);
        }

        return combined;
    }
}
