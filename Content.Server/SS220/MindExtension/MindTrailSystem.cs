using Content.Shared.Ghost;
using Content.Shared.SS220.MindExtension;
using Content.Shared.SS220.MindExtension.Events;

namespace Content.Server.SS220.MindExtension;


public partial class MindExtensionSystem : EntitySystem //MindTrailSystem
{
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

        if (_admin.IsAdmin(args.SenderSession) ||
            IsAvaibleToEnterEntity((EntityUid)target,
            mindExtEnt.Value.Comp,
            args.SenderSession.UserId) != BodyStateToEnter.Avaible)
            return;

        _mind.TransferTo((EntityUid)mind, (EntityUid)target);
        _mind.UnVisit((EntityUid)mind);
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
            var state = IsAvaibleToEnterEntity(pair.Key, mindExt, args.SenderSession.UserId);

            bodyList.Add(new TrailPoint(
                GetNetEntity(pair.Key),
                pair.Value,
                state,
                _admin.IsAdmin(args.SenderSession)));
        }

        RaiseNetworkEvent(new GhostBodyListResponse(bodyList), args.SenderSession.Channel);
    }
}
