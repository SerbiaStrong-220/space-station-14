using Content.Server.Mind;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Forensics.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.SS220.GhostExtension;
using Content.Shared.SS220.Mind;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.GhostExtension;
public sealed class GhostExtensionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    public override void Initialize()
    {
        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeNetworkEvent<ExtensionReturnActionEvent>(OnExtensionReturnActionEvent);
        SubscribeNetworkEvent<GhostBodyListRequestEvent>(OnGhostBodyListRequestEvent);
        SubscribeNetworkEvent<ExtensionRespawnActionEvent>(OnRespawnActionEvent);
        SubscribeLocalEvent<MindTransferedEvent>(OnMindTransferedEvent);

        //TODO: Установить связь таймера с UI. Выявлять суицидника.
    }

    //TODO: Задумайся, оно вообще надо?
    private List<Entity<MindExtensionComponent>> _timers = new();
    private void OnExtensionReturnActionEvent(ExtensionReturnActionEvent ev, EntitySessionEventArgs args)
    {
        var target = GetEntity(ev.Target);

        var mind = _mindSystem.GetMind(args.SenderSession.UserId);

        if (target is null || mind is null || !ValidateEntity((EntityUid)target, args))
            return;

        _mindSystem.TransferTo((EntityUid)mind, (EntityUid)target);
    }

    private void OnGhostBodyListRequestEvent(GhostBodyListRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } entity
                || !_ghostQuery.HasComp(entity))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");

            RaiseNetworkEvent(new GhostBodyListResponseEvent([]), args.SenderSession.Channel);
            return;
        }

        if (!TryComp<MindExtensionContainerComponent>(args.SenderSession.AttachedEntity, out var mindContExt))
        {
            RaiseNetworkEvent(new GhostBodyListResponseEvent([]), args.SenderSession.Channel);
            return;
        }

        if (!TryComp<MindExtensionComponent>(mindContExt.MindExtension, out var mindExt))
        {
            RaiseNetworkEvent(new GhostBodyListResponseEvent([]), args.SenderSession.Channel);
            return;
        }

        var bodyList = new List<BodyCont>();
        foreach (var ent in mindExt.Trail)
        {
            string entInfo;
            bool isAvaible = ValidateEntity(ent, args);

            if (_entityManager.EntityExists(ent))
            {
                entInfo = $"{Comp<MetaDataComponent>(ent).EntityName}";
            }
            else
            {
                entInfo = "(DELETED)";

            }

            bodyList.Add(new BodyCont(GetNetEntity(ent), entInfo, isAvaible));
        }

        RaiseNetworkEvent(new GhostBodyListResponseEvent(bodyList), args.SenderSession.Channel);
    }

    private void OnRespawnActionEvent(ExtensionRespawnActionEvent ev, EntitySessionEventArgs args)
    {
        if (!TryComp<MindExtensionContainerComponent>(args.SenderSession.AttachedEntity, out var mindContExt))
        {
            return;
        }

        if (!TryComp<MindExtensionComponent>(mindContExt.MindExtension, out var mindExt))
        {
            return;
        }

        if (!mindExt.IsIC && mindExt.RespawnTimer < _gameTiming.CurTime)
            RaiseLocalEvent((EntityUid)args.SenderSession.AttachedEntity, new RespawnActionEvent());
    }
    private void OnMindTransferedEvent(ref MindTransferedEvent ev)
    {
        if (ev.NewEntity is null)
            return;

        var mindExts = _entityManager.AllComponents<MindExtensionComponent>();
        var user = ev.PlayerSession;
        var mindExtEnt = mindExts.FirstOrNull(x => x.Component.PlayerSession == user);

        if (ev.PlayerSession is null)
            throw new NotImplementedException();

        if (mindExtEnt is null)
        {
            var newEnt = _entityManager.CreateEntityUninitialized(null);
            var mindExtComponent = new MindExtensionComponent() { PlayerSession = (NetUserId)ev.PlayerSession };

            _entityManager.AddComponent(newEnt, mindExtComponent);
            _entityManager.InitializeEntity(newEnt);
            mindExtEnt = new(newEnt, mindExtComponent);
        }

        var mindExt = mindExtEnt.Value.Component;
        var mindExtCont = new MindExtensionContainerComponent() { MindExtension = mindExtEnt.Value.Uid };

        if (TryComp<MindExtensionContainerComponent>(ev.OldEntity, out var oldMindExt))
        {
            _entityManager.RemoveComponent<MindExtensionComponent>((EntityUid)ev.OldEntity);
        }

        if (!TryComp<GhostComponent>(ev.NewEntity, out var ghost))
            mindExt.Trail.Add((EntityUid)ev.NewEntity);

        if (!TryComp<DnaComponent>(ev.NewEntity, out var dna))
        {
            if (mindExt.IsIC == true)
                mindExt.RespawnTimer = _gameTiming.CurTime + TimeSpan.FromSeconds(mindExt.RespawnAccumulatorMax);

            mindExt.IsIC = false;
        }
        else
            mindExt.IsIC = true;

        if (TryComp<MindExtensionContainerComponent>(ev.NewEntity, out var newMindExt))
        {
            newMindExt.MindExtension = mindExtCont.MindExtension;
        }
        _entityManager.AddComponent((EntityUid)ev.NewEntity, mindExtCont);
    }
    private bool ValidateEntity(EntityUid entity, EntitySessionEventArgs args)
    {
        bool isAvaible = true;

        if (!_entityManager.EntityExists(entity))
            isAvaible = false;

        if (TryComp<CryostorageContainedComponent>(entity, out var cryo))
            isAvaible = false;

        //При Visit MindConatainer может остаться, как и Mind. Нужно проверить, не является-ли этот Mind своим.
        if (TryComp<MindContainerComponent>(entity, out var mindContainer) && mindContainer.Mind is not null)
            if (TryComp<MindComponent>(mindContainer.Mind, out var mind) && mind.UserId != args.SenderSession.UserId)
                isAvaible = false;

        return isAvaible;
    }
}

[ByRefEvent]
public record struct MindTransferedEvent(EntityUid? NewEntity, EntityUid? OldEntity, NetUserId? PlayerSession);
