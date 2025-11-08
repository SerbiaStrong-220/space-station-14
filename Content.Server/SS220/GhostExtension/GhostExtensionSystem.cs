using Content.Server.Mind;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.SS220.Mind;

namespace Content.Server.SS220.GhostExtension;
public sealed class GhostExtensionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    public override void Initialize()
    {
        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeNetworkEvent<ExtensionReturnActionEvent>(OnExtensionReturnActionEvent);
        SubscribeNetworkEvent<GhostBodyListRequestEvent>(OnGhostBodyListRequestEvent);
        SubscribeNetworkEvent<ExtensionRespawnActionEvent>(OnRespawnActionEvent);
        SubscribeLocalEvent<MindTransferedEvent>(OnMindTransferedEvent);
    }

    private void OnExtensionReturnActionEvent(ExtensionReturnActionEvent ev, EntitySessionEventArgs args)
    {
        var target = GetEntity(ev.Target);

        var mind = _mindSystem.GetMind(args.SenderSession.UserId);

        if (target is null || mind is null || !ValidateEntity((EntityUid)target))
            return;

        _mindSystem.TransferTo((EntityUid)mind, (EntityUid)target);
    }

    private void OnGhostBodyListRequestEvent(GhostBodyListRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } entity
                || !_ghostQuery.HasComp(entity))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");
            return;
        }

        if (!TryComp<MindExtensionComponent>(args.SenderSession.AttachedEntity, out var mindExt))
            return;

        var bodyList = new List<BodyCont>();
        foreach (var ent in mindExt.Trail)
        {
            string entInfo;
            bool isAvaible = ValidateEntity(ent);

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

    private void OnRespawnActionEvent(ExtensionRespawnActionEvent ev)
    {
        if (TryGetEntity(ev.Invoker, out var entity))
            RaiseLocalEvent((EntityUid)entity, new RespawnActionEvent());
    }

    private void OnMindTransferedEvent(ref MindTransferedEvent ev)
    {
        var trail = new HashSet<EntityUid>();

        if (TryComp<MindExtensionComponent>(ev.OldEntity, out var oldMindExt))
        {
            trail = oldMindExt.Trail;
            _entityManager.RemoveComponent<MindExtensionComponent>((EntityUid)ev.OldEntity);
        }

        if (ev.NewEntity is null)
            return;

        if (!TryComp<GhostComponent>(ev.NewEntity, out var ghost))
            trail.Add((EntityUid)ev.NewEntity);

        if (TryComp<MindExtensionComponent>(ev.NewEntity, out var newMindExt))
        {
            newMindExt.Trail = trail;
        }
        else
        {
            _entityManager.AddComponent((EntityUid)ev.NewEntity, new MindExtensionComponent() { Trail = trail });
        }
    }
    private bool ValidateEntity(EntityUid entity)
    {
        bool isAvaible = true;

        if (!_entityManager.EntityExists(entity))
            isAvaible = false;

        if (TryComp<CryostorageContainedComponent>(entity, out var cryo))
            isAvaible = false;

        if (TryComp<MindContainerComponent>(entity, out var mind) && mind.Mind is not null)
            isAvaible = false;

        return isAvaible;
    }
}

[ByRefEvent]
public record struct MindTransferedEvent(EntityUid? NewEntity, EntityUid? OldEntity);
