using Content.Server.Administration.Managers;
using Content.Server.Mind;
using Content.Server.Silicons.Borgs;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.MindExtension;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.MindExtension;

public sealed partial class MindExtensionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly BorgSystem _borg = default!;
    [Dependency] private readonly ContainerSystem _container = default!;


    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<MindExtensionComponent> _mindExtQuery;
    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _mindExtQuery = GetEntityQuery<MindExtensionComponent>();

        SubscribeRespawnSystemEvents();
        SubscribeTrailSystemEvents();
        SubscribeTransferSystemEvents();
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

        entity = _mindExtQuery.Get(container.MindExtension.Value);

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

    #endregion
}

[ByRefEvent]
public record struct MindTransferedEvent(EntityUid? NewEntity, EntityUid? OldEntity, NetUserId? Player);

[ByRefEvent]
public record struct SuicidedEvent(EntityUid Invoker, NetUserId Player);

[ByRefEvent]
public record struct GhostedEvent(EntityUid OldEntity, bool CanReturn);
