using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Mind;
using Content.Server.Silicons.Borgs;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.MindExtension;
using Content.Shared.SS220.MindExtension.Events;
using Robust.Server.Containers;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.MindExtension;

public sealed partial class MindExtensionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly BorgSystem _borg = default!;
    [Dependency] private readonly ContainerSystem _container = default!;


    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<MindExtensionComponent> _mindExtQuery;
    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _mindExtQuery = GetEntityQuery<MindExtensionComponent>();

        SubscribeNetworkEvent<ExtensionRespawnActionEvent>(OnRespawnActionEvent);

        SubscribeNetworkEvent<ExtensionReturnActionEvent>(OnExtensionReturnActionEvent);
        SubscribeNetworkEvent<GhostBodyListRequest>(OnGhostBodyListRequestEvent);

        SubscribeLocalEvent<MindTransferedEvent>(OnMindTransferedEvent);
        SubscribeLocalEvent<SuicidedEvent>(OnSuicidedEvent);
        SubscribeLocalEvent<GhostedEvent>(OnGhostedEvent);
    }

    public Entity<MindExtensionComponent> GetMindExtension(NetUserId player)
    {
        var mindExts = _entityManager.AllComponents<MindExtensionComponent>();
        var entity = mindExts.FirstOrNull(x => x.Component.PlayerSession == player);

        if (entity is not null)
            return (Entity<MindExtensionComponent>)entity;

        return CreateExtensionEntity(player);
    }
    public bool TryGetMindExtension(NetUserId player, [NotNullWhen(true)] out Entity<MindExtensionComponent>? entity)
    {
        var mindExts = _entityManager.AllComponents<MindExtensionComponent>();
        entity = mindExts.FirstOrNull(x => x.Component.PlayerSession == player);

        return entity is not null;
    }

    public bool TryGetMindExtension(MindExtensionContainerComponent container, [NotNullWhen(true)] out Entity<MindExtensionComponent>? entity)
    {
        entity = null;

        if (container.MindExtension is null)
            return false;

        entity = _mindExtQuery.Get((EntityUid)container.MindExtension);

        return entity is not null;
    }

    private (EntityUid Uid, MindExtensionComponent Component) CreateExtensionEntity(NetUserId playerSession)
    {
        var newEnt = _entityManager.CreateEntityUninitialized(null);
        var mindExtComponent = new MindExtensionComponent() { PlayerSession = playerSession };

        _entityManager.AddComponent(newEnt, mindExtComponent);
        _entityManager.InitializeEntity(newEnt);
        return new(newEnt, mindExtComponent);
    }

    #region Helpers
    private bool CheckEntityAbandoned(EntityUid entity)
    {
        //Если ливаем не из тела, то норм.
        if (!TryComp<MobStateComponent>(entity, out var mobState))
            return false;

        //Если ливаем из мертвого тела, то норм.
        switch (mobState.CurrentState)
        {
            case Shared.Mobs.MobState.Invalid:
                return false;
            case Shared.Mobs.MobState.Dead:
                return false;
        }

        //Если ливаем из живого тела, то пока пиздося с гарантом, кроме случая суицида.
        return true;
    }

    private void ChangeOrAddTrailPoint(MindExtensionComponent comp, EntityUid entity, bool isAbandoned)
    {
        if (HasComp<GhostComponent>(entity))
            return;

        if (TryComp<BorgChassisComponent>(entity, out var chassisComp))
        {
            if (chassisComp?.BrainContainer.ContainedEntity is null)
                return;

            entity = (EntityUid)chassisComp.BrainContainer.ContainedEntity;
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

    #endregion
}

[ByRefEvent]
public record struct MindTransferedEvent(EntityUid? NewEntity, EntityUid? OldEntity, NetUserId? Player);

[ByRefEvent]
public record struct SuicidedEvent(EntityUid Invoker, NetUserId Player);

[ByRefEvent]
public record struct GhostedEvent(EntityUid OldEntity, bool CanReturn);
