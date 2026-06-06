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

    /// <inheritdoc cref="RegisterTarget(ProtoId{QuadHearingTargetPrototype}, EntityCoordinates, float?, ICommonSession?)"/>
    public void RegisterTarget(ProtoId<QuadHearingTargetPrototype> protoId, EntityUid target, EntityUid? predictedUser)
    {
        RegisterTarget(protoId, new EntityCoordinates(target, Vector2.Zero), predictedUser);
    }

    /// <inheritdoc cref="RegisterTarget(ProtoId{QuadHearingTargetPrototype}, EntityCoordinates, float?, ICommonSession?)"/>
    public void RegisterTarget(ProtoId<QuadHearingTargetPrototype> protoId, EntityUid target, ICommonSession? predictedSession = null)
    {
        RegisterTarget(protoId, new EntityCoordinates(target, Vector2.Zero), predictedSession);
    }

    /// <inheritdoc cref="RegisterTarget(ProtoId{QuadHearingTargetPrototype}, EntityCoordinates, float?, ICommonSession?)"/>
    public void RegisterTarget(ProtoId<QuadHearingTargetPrototype> protoId, MapCoordinates coords, EntityUid? predictedUser)
    {
        if (!_map.TryGetMap(coords.MapId, out var map))
            return;

        RegisterTarget(protoId, new EntityCoordinates(map.Value, coords.Position), predictedUser);
    }

    /// <inheritdoc cref="RegisterTarget(ProtoId{QuadHearingTargetPrototype}, EntityCoordinates, float?, ICommonSession?)"/>
    public void RegisterTarget(ProtoId<QuadHearingTargetPrototype> protoId, MapCoordinates coords, ICommonSession? predictedSession = null)
    {
        if (!_map.TryGetMap(coords.MapId, out var map))
            return;

        RegisterTarget(protoId, new EntityCoordinates(map.Value, coords.Position), predictedSession);
    }

    /// <inheritdoc cref="RegisterTarget(ProtoId{QuadHearingTargetPrototype}, EntityCoordinates, float?, ICommonSession?)"/>
    public void RegisterTarget(ProtoId<QuadHearingTargetPrototype> protoId, EntityCoordinates coords, EntityUid? predictedUser)
    {
        ICommonSession? session = null;
        if (predictedUser != null)
            _player.TryGetSessionByEntity(predictedUser.Value, out session);

        RegisterTarget(protoId, coords, session);
    }

    /// <summary>
    /// Registers the coordinates of the overlay target.
    /// </summary>
    public abstract void RegisterTarget(ProtoId<QuadHearingTargetPrototype> protoId, EntityCoordinates coords, ICommonSession? predictedSession = null);

    protected EntityCoordinates ToMapOrGridCoordinates(EntityCoordinates coords)
    {
        var mapCoords = _transform.ToMapCoordinates(coords);
        if (_mapMng.TryFindGridAt(mapCoords, out var gridUid, out _) && !TerminatingOrDeleted(gridUid))
            return gridUid == coords.EntityId ? coords : _transform.ToCoordinates(gridUid, mapCoords);

        var map = _map.GetMap(mapCoords.MapId);
        return new EntityCoordinates(map, mapCoords.Position);
    }

    protected bool CanRegisterTarget(Entity<QuadHearingComponent> recipient, MapCoordinates targetMapCoords, float range)
    {
        var recipientMapCoords = _transform.GetMapCoordinates(recipient.Owner);

        if (recipientMapCoords.MapId != targetMapCoords.MapId)
            return false;

        var delta = recipientMapCoords.Position - targetMapCoords.Position;
        var distanceSquared = delta.LengthSquared();
        if (distanceSquared < recipient.Comp.MinDistance * recipient.Comp.MinDistance || distanceSquared > range * range)
            return false;

        return true;
    }

    protected static float GetHearingRange(QuadHearingTargetPrototype proto, Entity<QuadHearingComponent> recipient)
    {
        if (recipient.Comp.HearingRangeOverride is { } ovr && ovr.TryGetValue(proto.ID, out var ovrRange))
            return ovrRange;

        return proto.HearingRange;
    }
}

[Serializable, NetSerializable]
public sealed class QuadHearingRegisterTargetMessage : EntityEventArgs
{
    public required ProtoId<QuadHearingTargetPrototype> ProtoId;
    public required NetCoordinates Coordinates;
}

[DataDefinition]
public sealed partial class ToggleQuadHearingEvent : InstantActionEvent { }
