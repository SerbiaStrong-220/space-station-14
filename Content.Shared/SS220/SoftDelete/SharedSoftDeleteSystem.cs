// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.GameTicking;
using Robust.Shared.Map;

namespace Content.Shared.SS220.SoftDelete;

/// <summary>
/// Allows to fully disable an entity without completely deleting it, and then restore it back.
/// </summary>
public sealed class SharedSoftDeleteSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private EntityUid? PausedMap { get; set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public bool IsSoftDeleted(Entity<TransformComponent?> entity)
    {
        var (_, comp) = entity;
        comp ??= Transform(entity);

        return comp.MapUid != null && comp.MapUid == PausedMap;
    }

    public void SoftDelete(EntityUid entity)
    {
        EnsurePausedMap();
        if (PausedMap is not { } pausedMap)
        {
            Log.Error("Soft delete map was unexpectedly null");
            return;
        }
        var transform = Transform(entity);
        if (IsSoftDeleted((entity, transform)))
            return;
        var parent = _transformSystem.GetParentUid(entity);
        var softDeletedComp = EnsureComp<SoftDeletedComponent>(entity);
        softDeletedComp.ActualParent = parent;
        _transformSystem.SetParent(entity, transform, pausedMap);
        DirtyEntity(entity);
    }

    public bool TryRestore(EntityUid entity)
    {
        var transform = Transform(entity);
        if (!IsSoftDeleted((entity, transform)))
            return false;
        if (!TryComp<SoftDeletedComponent>(entity, out var softDeletedComp))
        {
            Log.Error($"Expected {nameof(SoftDeletedComponent)} on soft deleted entity, found null");
            return false;
        }
        _transformSystem.SetParent(entity, transform, softDeletedComp.ActualParent);
        RemComp(entity, softDeletedComp);
        return true;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        DeletePausedMap();
    }

    private void DeletePausedMap()
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        EntityManager.DeleteEntity(PausedMap.Value);
        PausedMap = null;
    }

    private void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        PausedMap = _mapSystem.CreateMap(out var mapId);
        _mapManager.SetMapPaused(mapId, true);
    }
}
