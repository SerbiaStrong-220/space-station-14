using Content.Server.Power.EntitySystems;
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
        //Нужно найти MindExtension и сверить с Trail. Сверить с IsAvaible.
        if (!TryGetMindExtension(args.SenderSession.UserId, out var mindExtEnt))
            return;

        //Нужно проверить целевую энтити на легальность интервенции.
        if (!TryGetEntity(ev.Target, out var target))
            return;

        //Нужно найти mind, который нужно будет переместить.
        if (!_mind.TryGetMind(args.SenderSession.UserId, out var mind))
            return;

        if (!_admin.IsAdmin(args.SenderSession) &&
            IsAvaibleToEnterEntity(target.Value,
            mindExtEnt.Value.Comp,
            args.SenderSession.UserId) != BodyStateToEnter.Avaible)
            return;

        _mind.TransferTo(mind.Value, target.Value);
        _mind.UnVisit(mind.Value);
    }

    private void OnGhostBodyListRequestEvent(GhostBodyListRequest ev, EntitySessionEventArgs args)
    {
        // Нужно проверить наличие компонента-контейнера и компонента MindExtension.
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
            var state = IsAvaibleToEnterEntity(pair.Key, mindExt, args.SenderSession.UserId);

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

    private BodyStateToEnter IsAvaibleToEnterEntity(
        EntityUid target,
        MindExtensionComponent mindExtension,
        NetUserId session)
    {

        if (!_entityManager.EntityExists(target))
            return BodyStateToEnter.Destroyed;

        if (TryComp<CryostorageContainedComponent>(target, out var cryo))
            return BodyStateToEnter.InCryo;

        //При Visit MindConatainer может остаться, как и Mind. Нужно проверить, не является-ли этот Mind своим.
        //Если Mind не свой, значит тело занято.
        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind is not null)
            if (TryComp<MindComponent>(mindContainer.Mind, out var mind) && mind.UserId != session)
                return BodyStateToEnter.Engaged;

        if (mindExtension.Trail.TryGetValue(target, out var metaData))
        {
            if (metaData.IsAbandoned)
                return BodyStateToEnter.Abandoned;
        }

        return BodyStateToEnter.Avaible;
    }
}
