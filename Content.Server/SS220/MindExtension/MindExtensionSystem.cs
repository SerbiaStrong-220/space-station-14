// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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

/// <summary>
/// The Entity System writes all player transfers from entity to entity.
/// It allows the player to return to a recorded entity.
/// System also manages the player's return to the lobby.
/// </summary>
public sealed partial class MindExtensionSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly BorgSystem _borg = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    private EntityQuery<MindExtensionComponent> _mindExtQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mindExtQuery = GetEntityQuery<MindExtensionComponent>();

        SubscribeRespawnSystemEvents();
        SubscribeTrailSystemEvents();
    }

    /// <summary>
    /// Returns the player associated with entity of <see cref="MindExtensionComponent"/>.
    /// If it doesn't exist, it will be created.
    /// </summary>
    public Entity<MindExtensionComponent> GetMindExtension(NetUserId player)
    {
        var mindExts = EntityManager.AllComponents<MindExtensionComponent>();
        var entity = mindExts.FirstOrNull(x => x.Component.Player == player);

        if (entity is not null)
            return entity.Value;

        var newEnt = EntityManager.CreateEntityUninitialized(null);
        var mindExtComponent = new MindExtensionComponent() { Player = player };

        EntityManager.AddComponent(newEnt, mindExtComponent);
        EntityManager.InitializeEntity(newEnt);
        return new(newEnt, mindExtComponent);
    }

    public bool TryGetMindExtension(NetUserId player, [NotNullWhen(true)] out Entity<MindExtensionComponent>? entity)
    {
        var mindExts = EntityManager.AllComponents<MindExtensionComponent>();
        entity = mindExts.FirstOrNull(x => x.Component.Player == player);

        return entity is not null;
    }

    public bool TryGetMindExtension(MindExtensionContainerComponent container,
        [NotNullWhen(true)] out Entity<MindExtensionComponent>? entity)
    {
        entity = null;

        if (container.MindExtension is null)
            return false;

        entity = _mindExtQuery.Get(container.MindExtension.Value);

        return entity is not null;
    }

    /// <summary>
    /// It performs the main check of the system to determine
    /// whether the entity will be permanently abandoned by the player.
    /// </summary>
    private bool CheckEntityAbandoned(EntityUid entity)
    {
        //An entity is only considered abandoned if
        //it had a MobStateComponent and a MobState of Alive or Critical
        //at the time of transfer.

        //Note: suicide is handled separately and ignores this rule.

        if (!TryComp<MobStateComponent>(entity, out var mobState))
            return false;

        switch (mobState.CurrentState)
        {
            case Shared.Mobs.MobState.Invalid:
                return false;
            case Shared.Mobs.MobState.Dead:
                return false;
        }

        return true;
    }
}
