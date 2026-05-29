using Content.Shared.Actions;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.SS220.QuadHearing;

public abstract class SharedQuadHearingSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMng = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<QuadHearingComponent, ToggleQuadHearingEvent>(OnToggleAction);
    }

    private void OnToggleAction(Entity<QuadHearingComponent> ent, ref ToggleQuadHearingEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.ShowEffect = !ent.Comp.ShowEffect;
        Dirty(ent);

        args.Handled = true;
    }

    public void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, EntityUid target, float? range, EntityUid? predictedUser)
    {
        RegisterTarget(protoId, new EntityCoordinates(target, Vector2.Zero), range, predictedUser);
    }

    public void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, EntityUid target, float? range, ICommonSession? predictedSession = null)
    {
        RegisterTarget(protoId, new EntityCoordinates(target, Vector2.Zero), range, predictedSession);
    }

    public void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, MapCoordinates coords, float? range, EntityUid? predictedUser)
    {
        var map = _map.GetMap(coords.MapId);
        RegisterTarget(protoId, new EntityCoordinates(map, coords.Position), range, predictedUser);
    }

    public void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, MapCoordinates coords, float? range, ICommonSession? predictedSession = null)
    {
        var map = _map.GetMap(coords.MapId);
        RegisterTarget(protoId, new EntityCoordinates(map, coords.Position), range, predictedSession);
    }

    public void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, EntityCoordinates coords, float? range, EntityUid? predictedUser)
    {
        ICommonSession? session = null;
        if (predictedUser != null)
            _player.TryGetSessionByEntity(predictedUser.Value, out session);

        RegisterTarget(protoId, coords, range, session);
    }

    public abstract void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, EntityCoordinates coords, float? range, ICommonSession? predictedSession = null);

    protected EntityCoordinates ToMapOrGridCoordinated(EntityCoordinates coords)
    {
        var mapCoords = _transform.ToMapCoordinates(coords);
        if (_mapMng.TryFindGridAt(mapCoords, out var gridUid, out _) && !TerminatingOrDeleted(gridUid))
            return gridUid == coords.EntityId ? coords : _transform.ToCoordinates(gridUid, mapCoords);

        var map = _map.GetMap(mapCoords.MapId);
        return new EntityCoordinates(map, mapCoords.Position);
    }

    protected bool CanRegisterTarget(Entity<QuadHearingComponent> recepient, EntityCoordinates targetCoords, float? range)
    {
        var recepientMapCoords = _transform.GetMapCoordinates(recepient.Owner);
        var targetMapCoords = _transform.ToMapCoordinates(targetCoords);

        if (recepientMapCoords.MapId != targetMapCoords.MapId)
            return false;

        var delta = recepientMapCoords.Position - targetMapCoords.Position;
        var distance = delta.Length();
        if (distance > range || distance < recepient.Comp.MinDistance)
            return false;

        return true;
    }
}

[Serializable, NetSerializable]
public sealed class QuadHearingRegisterTargetMessage : EntityEventArgs
{
    public required ProtoId<QuadHearingTargetTypePrototype> ProtoId;
    public required NetCoordinates Coordinates;
}

[DataDefinition]
public sealed partial class ToggleQuadHearingEvent : InstantActionEvent { }
