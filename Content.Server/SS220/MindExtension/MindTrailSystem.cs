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
        SubscribeNetworkEvent<ExtensionReturnActionEvent>(OnExtensionReturnActionEvent);
        SubscribeNetworkEvent<GhostBodyListRequest>(OnGhostBodyListRequestEvent);
        SubscribeNetworkEvent<DeleteTrailPointRequest>(OnDeleteTrailPointRequest);
    }

    #region Handlers

    private void OnExtensionReturnActionEvent(ExtensionReturnActionEvent ev, EntitySessionEventArgs args)
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
        foreach (var pair in mindExt.Trail)
        {
            EntityUid target = pair.Key;
            TrailPointMetaData trailMetaData = pair.Value;
            var state = IsAvailableToEnterEntity(pair.Key, mindExt, args.SenderSession.UserId);

            if (TryComp<BorgBrainComponent>(pair.Key, out var borgBrain))
            {
                if (_container.TryGetContainingContainer(pair.Key, out var container) &&
                    HasComp<BorgChassisComponent>(container.Owner))
                {
                    target = container.Owner;

                    var metaData = Comp<MetaDataComponent>(target);

                    trailMetaData.EntityName = metaData.EntityName;
                    trailMetaData.EntityDescription = $"({Loc.GetString("mind-ext-borg-contained",
                        ("borgname", pair.Value.EntityName))}) {metaData.EntityDescription}";
                }
            }

            bodyList.Add(new TrailPoint(
                GetNetEntity(pair.Key),
                trailMetaData,
                state,
                _admin.IsAdmin(args.SenderSession)));
        }

        RaiseNetworkEvent(new GhostBodyListResponse(bodyList), args.SenderSession.Channel);
    }

    private void OnDeleteTrailPointRequest(DeleteTrailPointRequest ev, EntitySessionEventArgs args)
    {
        var mindExt = GetMindExtension(args.SenderSession.UserId);

        if (!TryGetEntity(ev.Entity, out var ent))
            return;

        if (mindExt.Comp.Trail.Remove(ent.Value))
        {
            var eventArgs = new DeleteTrailPointResponse(ev.Entity);
            RaiseNetworkEvent(eventArgs, args.SenderSession.Channel);
        }
    }

    #endregion

    private void ChangeOrAddTrailPoint(MindExtensionComponent comp, EntityUid entity, bool isAbandoned)
    {
        if (HasComp<GhostComponent>(entity))
            return;

        //If borg mind slot is not empty - write borg mind instead.
        if (TryComp<BorgChassisComponent>(entity, out var chassisComp))
        {
            if (chassisComp.BrainContainer.ContainedEntity is null)
                return;

            entity = chassisComp.BrainContainer.ContainedEntity.Value;
        }

        if (comp.Trail.ContainsKey(entity))
        {
            var trailMetaData = comp.Trail[entity];
            trailMetaData.IsAbandoned = isAbandoned;
            comp.Trail[entity] = trailMetaData;
            return;
        }

        TryComp(entity, out MetaDataComponent? metaData);

        comp.Trail.Add(entity, new TrailPointMetaData()
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

        if (!_entityManager.EntityExists(target))
            return BodyStateToEnter.Destroyed;

        if (TryComp<CryostorageContainedComponent>(target, out var cryo))
            return BodyStateToEnter.InCryo;

        //When visiting, the MindConatainer may remain, as may the Mind.
        //It's necessary to check whether this Mind is your own.
        //If the Mind isn't your own, then the body is occupied.
        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind is not null)
            if (TryComp<MindComponent>(mindContainer.Mind, out var mind) && mind.UserId != session)
                return BodyStateToEnter.Engaged;

        if (mindExtension.Trail.TryGetValue(target, out var metaData))
        {
            if (metaData.IsAbandoned)
                return BodyStateToEnter.Abandoned;
        }

        return BodyStateToEnter.Available;
    }
}
