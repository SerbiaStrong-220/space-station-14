using Content.Server.Mind;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.GhostExtension;
using Content.Shared.SS220.Mind;
using Content.Shared.SS220.MindExtension;
using Robust.Shared.Network;

namespace Content.Server.SS220.MindExtension;

public sealed class MindExtensionSystem : SharedMindExtensionSystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    [Dependency] private readonly MindRespawnSystem _mindRespawn = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeNetworkEvent<ExtensionReturnActionEvent>(OnExtensionReturnActionEvent);
        SubscribeNetworkEvent<GhostBodyListRequestEvent>(OnGhostBodyListRequestEvent);

        SubscribeLocalEvent<MindTransferedEvent>(OnMindTransferedEvent);
        SubscribeLocalEvent<SuicidedEvent>(OnSuicidedEvent);
        SubscribeLocalEvent<GhostedEvent>(OnGhostedEvent);
        //SubscribeLocalEvent<MindSwappedEvent>(OnMindSwappedEvent);
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

        if (IsAvaibleToEnterEntity((EntityUid)target, mindExtEnt.Value.Comp, args.SenderSession.UserId) != BodyStateToEnter.Avaible)
            return;

        _mind.TransferTo((EntityUid)mind, (EntityUid)target);
        _mind.UnVisit((EntityUid)mind);
    }
    private void OnGhostBodyListRequestEvent(GhostBodyListRequestEvent ev, EntitySessionEventArgs args)
    {
        // Нужно проверить, гост-ли отправляет запрос.
        if (args.SenderSession.AttachedEntity is not { Valid: true } entity
                || !_ghostQuery.HasComp(entity))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");

            RaiseNetworkEvent(new GhostBodyListResponseEvent([]), args.SenderSession.Channel);
            return;
        }

        // Нужно проверить наличие компонента-контейнера и компонента MindExtension.
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
        foreach (var pair in mindExt.Trail)
        {
            var state = IsAvaibleToEnterEntity(pair.Key, mindExt, args.SenderSession.UserId);

            bodyList.Add(new BodyCont(GetNetEntity(pair.Key), pair.Value, state));
        }

        RaiseNetworkEvent(new GhostBodyListResponseEvent(bodyList), args.SenderSession.Channel);
    }
    private void OnMindTransferedEvent(ref MindTransferedEvent ev)
    {
        if (ev.Player is null)
            return;

        TryComp<MetaDataComponent>(ev.OldEntity, out var metaData);

        //Нужно создать пустую энтитю с MindExtComp, если ее нет.
        if (!TryGetMindExtension((NetUserId)ev.Player, out var mindExtEnt))
            mindExtEnt = CreateExtensionEntity((NetUserId)ev.Player);

        var mindExt = mindExtEnt.Value.Comp;

        //На всякий удалить старый MindContainer.
        if (TryComp<MindExtensionContainerComponent>(ev.OldEntity, out var oldMindExt))
        {
            ChangeOrAddTrailPoint(mindExt, (EntityUid)ev.OldEntity, CheckEntityAbandoned((EntityUid)ev.OldEntity));
            _entityManager.RemoveComponent<MindExtensionContainerComponent>((EntityUid)ev.OldEntity);
        }

        if (ev.NewEntity is null)
            return;

        ChangeOrAddTrailPoint(mindExt, (EntityUid)ev.NewEntity, false);

        _mindRespawn.SetRespawnTimer(mindExt, (EntityUid)ev.NewEntity, (NetUserId)ev.Player);

        var mindExtCont = new MindExtensionContainerComponent() { MindExtension = mindExtEnt.Value.Owner };

        //Если компонент уже есть, просто заменить ему ИД на православный.
        if (TryComp<MindExtensionContainerComponent>(ev.NewEntity, out var newMindExt))
        {
            newMindExt.MindExtension = mindExtCont.MindExtension;
        }
        else
        {
            _entityManager.AddComponent((EntityUid)ev.NewEntity, mindExtCont);
        }
    }
    private void OnSuicidedEvent(ref SuicidedEvent ev)
    {
        Suicide(ev.Invoker, ev.Player);
    }
    private void OnGhostedEvent(ref GhostedEvent ev)
    {
        GhostAttempt(ev.OldEntity, ev.CanReturn);
    }
    #endregion
    public void GhostAttempt(EntityUid oldEntity, bool canReturn)
    {
        if (!TryComp<MindExtensionContainerComponent>(oldEntity, out var oldMindExt))
            return;

        if (!TryGetMindExtension(oldMindExt, out var mindExt))
            return;

        ChangeOrAddTrailPoint(mindExt, oldEntity, CheckEntityAbandoned(oldEntity));
    }
    public void Suicide(EntityUid invoker, NetUserId player)
    {
        // Если сущность жива, то делать нечего.
        if (!(_mobState.IsCritical(invoker) || _mobState.IsDead(invoker)))
            return;

        if (!TryGetMindExtension(player, out var mindExtEnt))
            return;

        ChangeOrAddTrailPoint(
            comp: mindExtEnt.Value.Comp,
            entity: invoker,
            // Все просто, сущность доступна при суициде в крите/после смерти.
            isAbandoned: false);
    }
}

[ByRefEvent]
public record struct MindTransferedEvent(EntityUid? NewEntity, EntityUid? OldEntity, NetUserId? Player);

[ByRefEvent]
public record struct SuicidedEvent(EntityUid Invoker, NetUserId Player);

[ByRefEvent]
public record struct GhostedEvent(EntityUid OldEntity, bool CanReturn);
