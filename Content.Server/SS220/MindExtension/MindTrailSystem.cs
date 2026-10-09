// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Bed.Cryostorage;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.MindExtension;
using Content.Shared.SS220.MindExtension.Events;
using Robust.Shared.Network;

namespace Content.Server.SS220.MindExtension;

public partial class MindExtensionSystem //MindTrailSystem
{
    private void SubscribeTrailSystemEvents()
    {
        SubscribeNetworkEvent<ExtensionReturnRequest>(OnExtensionReturnActionEvent);
        SubscribeNetworkEvent<GhostBodyListRequest>(OnGhostBodyListRequestEvent);
        SubscribeNetworkEvent<DeleteTrailPointRequest>(OnDeleteTrailPointRequest);
    }

    #region Handlers

    private void OnExtensionReturnActionEvent(ExtensionReturnRequest ev, EntitySessionEventArgs args)
    {
        if (!TryGetExtension(args.SenderSession.UserId, out var data))
            return;

        if (!TryGetEntity(ev.Target, out var target))
            return;

        if (!_mind.TryGetMind(args.SenderSession.UserId, out var mind))
            return;

        if (!_admin.IsAdmin(args.SenderSession) &&
            IsAvailableToEnterEntity(target.Value, data, args.SenderSession.UserId) != BodyStateToEnter.Available)
            return;

        _mind.TransferTo(mind.Value, target.Value);
        _mind.UnVisit(mind.Value);

        RaiseNetworkEvent(new ExtensionReturnResponse(), args.SenderSession);
    }

    private void OnGhostBodyListRequestEvent(GhostBodyListRequest ev, EntitySessionEventArgs args)
    {
        if (!TryGetExtension(args.SenderSession.UserId, out var data))
        {
            RaiseNetworkEvent(new GhostBodyListResponse([]), args.SenderSession.Channel);
            return;
        }

        var bodyList = new List<TrailPoint>();
        foreach (var (targetNet, trailMetaData) in data.Trail)
        {
            var targetEnt = GetEntity(targetNet);
            var finalMetaData = trailMetaData;
            var state = IsAvailableToEnterEntity(targetEnt, data, args.SenderSession.UserId);

            if (HasComp<BorgBrainComponent>(targetEnt))
            {
                if (_container.TryGetContainingContainer(targetEnt, out var container) &&
                    HasComp<BorgChassisComponent>(container.Owner))
                {
                    var chassis = container.Owner;
                    var meta = MetaData(chassis);
                    finalMetaData.EntityName = meta.EntityName;
                    finalMetaData.EntityDescription = $"({Loc.GetString("mind-ext-borg-contained",
                        ("borgname", trailMetaData.EntityName))}) {meta.EntityDescription}";
                }
            }

            bodyList.Add(new TrailPoint(targetNet, finalMetaData, state, _admin.IsAdmin(args.SenderSession)));
        }

        RaiseNetworkEvent(new GhostBodyListResponse(bodyList), args.SenderSession.Channel);
    }

    private void OnDeleteTrailPointRequest(DeleteTrailPointRequest ev, EntitySessionEventArgs args)
    {
        if (TryGetExtension(args.SenderSession.UserId, out var data) && data.Trail.Remove(ev.Entity))
            RaiseNetworkEvent(new DeleteTrailPointResponse(ev.Entity), args.SenderSession.Channel);
    }

    #endregion

    /// <summary>
    /// Adds a new entity to the players trail or updates the status of an existing one.
    /// </summary>
    private void ChangeOrAddTrailPoint(MindExtensionData data, EntityUid entity, bool isAbandoned)
    {
        if (HasComp<GhostComponent>(entity))
            return;

        if (TryComp<BorgChassisComponent>(entity, out var chassisComp) && chassisComp.BrainContainer.ContainedEntity != null)
            entity = chassisComp.BrainContainer.ContainedEntity.Value;

        var netEntity = GetNetEntity(entity);
        if (data.Trail.TryGetValue(netEntity, out var existing))
        {
            existing.IsAbandoned = isAbandoned;
            data.Trail[netEntity] = existing;
            return;
        }

        var meta = MetaData(entity);

        var trailData = new TrailPointMetaData
        {
            EntityName = meta.EntityName,
            EntityDescription = meta.EntityDescription,
            IsAbandoned = isAbandoned,
        };

        data.Trail.Add(netEntity, trailData);
    }

    /// <summary>
    /// Main check is whether one can return to the essence.
    /// </summary>
    private BodyStateToEnter IsAvailableToEnterEntity(EntityUid target, MindExtensionData data, NetUserId user)
    {
        if (!Exists(target))
            return BodyStateToEnter.Destroyed;

        if (HasComp<CryostorageContainedComponent>(target))
            return BodyStateToEnter.InCryo;

        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind != null)
        {
            if (TryComp<MindComponent>(mindContainer.Mind, out var mind) && mind.UserId != null && mind.UserId != user)
                return BodyStateToEnter.Engaged;
        }

        if (data.Trail.TryGetValue(GetNetEntity(target), out var meta) && meta.IsAbandoned)
            return BodyStateToEnter.Abandoned;

        return BodyStateToEnter.Available;
    }
}
