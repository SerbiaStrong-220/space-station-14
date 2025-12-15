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

public partial class MindExtensionSystem : EntitySystem //MindTrailSystem
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
        if (!TryGetMindExtension(args.SenderSession.UserId, out var mindExtEnt))
            return;

        if (!TryGetEntity(ev.Target, out var target))
            return;

        if (!_mind.TryGetMind(args.SenderSession.UserId, out var mind))
            return;

        if (!_admin.IsAdmin(args.SenderSession) &&
            IsAvailableToEnterEntity(target.Value,
            mindExtEnt.Value.Comp,
            args.SenderSession.UserId) != BodyStateToEnter.Available)
            return;

        _mind.TransferTo(mind.Value, target.Value);
        _mind.UnVisit(mind.Value);

        RaiseNetworkEvent(new ExtensionReturnResponse(), args.SenderSession);
    }

    private void OnGhostBodyListRequestEvent(GhostBodyListRequest ev, EntitySessionEventArgs args)
    {
        if (!TryComp<MindExtensionContainerComponent>(args.SenderSession.AttachedEntity, out var mindContExt))
        {
            RaiseNetworkEvent(new GhostBodyListResponse([]), args.SenderSession.Channel);
            return;
        }

        if (!TryComp<MindExtensionComponent>(mindContExt.MindExtension, out var mindExt))
        {
            RaiseNetworkEvent(new GhostBodyListResponse([]), args.SenderSession.Channel);
            return;
        }

        var bodyList = new List<TrailPoint>();
        foreach (var (target, trailMetaData) in mindExt.Trail)
        {
            TrailPointMetaData finalMetaData = trailMetaData;
            var targetEnt = GetEntity(target);

            var state = IsAvailableToEnterEntity(targetEnt, mindExt, args.SenderSession.UserId);

            if (TryComp<BorgBrainComponent>(targetEnt, out var borgBrain))
            {
                if (_container.TryGetContainingContainer(targetEnt, out var container) &&
                    HasComp<BorgChassisComponent>(container.Owner))
                {
                    targetEnt = container.Owner;

                    var metaData = Comp<MetaDataComponent>(targetEnt);

                    finalMetaData.EntityName = metaData.EntityName;
                    finalMetaData.EntityDescription = $"({Loc.GetString("mind-ext-borg-contained",
                        ("borgname", trailMetaData.EntityName))}) {metaData.EntityDescription}";
                }
            }

            bodyList.Add(new TrailPoint(
                target,
                finalMetaData,
                state,
                _admin.IsAdmin(args.SenderSession)));
        }

        RaiseNetworkEvent(new GhostBodyListResponse(bodyList), args.SenderSession.Channel);
    }

    private void OnDeleteTrailPointRequest(DeleteTrailPointRequest ev, EntitySessionEventArgs args)
    {
        var mindExt = GetMindExtension(args.SenderSession.UserId);

        if (mindExt.Comp.Trail.Remove(ev.Entity))
        {
            var eventArgs = new DeleteTrailPointResponse(ev.Entity);
            RaiseNetworkEvent(eventArgs, args.SenderSession.Channel);
        }
    }

    #endregion

    private void ChangeOrAddTrailPoint(MindExtensionComponent comp, EntityUid entity, bool isAbandoned)
    {
        var netEntity = GetNetEntity(entity);

        if (HasComp<GhostComponent>(entity))
            return;

        //If borg mind slot is not empty - write borg mind instead.
        if (TryComp<BorgChassisComponent>(entity, out var chassisComp))
        {
            if (chassisComp.BrainContainer.ContainedEntity is null)
                return;

            entity = chassisComp.BrainContainer.ContainedEntity.Value;
            netEntity = GetNetEntity(chassisComp.BrainContainer.ContainedEntity.Value);
        }

        if (comp.Trail.ContainsKey(netEntity))
        {
            var trailMetaData = comp.Trail[netEntity];
            trailMetaData.IsAbandoned = isAbandoned;
            comp.Trail[netEntity] = trailMetaData;
            return;
        }

        TryComp(entity, out MetaDataComponent? metaData);

        comp.Trail.Add(netEntity, new TrailPointMetaData()
        {
            EntityName = metaData?.EntityName ?? "",
            EntityDescription = metaData?.EntityDescription ?? "",
            IsAbandoned = isAbandoned
        });
    }

    /// <summary>
    /// Main check is whether one can return to the essence.
    /// </summary>
    private BodyStateToEnter IsAvailableToEnterEntity(
        EntityUid target,
        MindExtensionComponent mindExtension,
        NetUserId session)
    {

        if (!EntityManager.EntityExists(target))
            return BodyStateToEnter.Destroyed;

        if (TryComp<CryostorageContainedComponent>(target, out var cryo))
            return BodyStateToEnter.InCryo;

        //When visiting, the MindConatainer may remain, as may the Mind.
        //It's necessary to check whether this Mind is your own.
        //If the Mind isn't your own, then the body is occupied.
        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind is not null)
            if (TryComp<MindComponent>(mindContainer.Mind, out var mind) && mind.UserId != session)
                return BodyStateToEnter.Engaged;

        if (mindExtension.Trail.TryGetValue(GetNetEntity(target), out var metaData))
        {
            if (metaData.IsAbandoned)
                return BodyStateToEnter.Abandoned;
        }

        return BodyStateToEnter.Available;
    }
}
