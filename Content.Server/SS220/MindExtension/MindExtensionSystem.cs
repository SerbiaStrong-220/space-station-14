// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Managers;
using Content.Server.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.MindExtension;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using JetBrains.Annotations;

namespace Content.Server.SS220.MindExtension;

/// <summary>
/// The Entity System writes all player transfers from entity to entity.
/// It allows the player to return to a recorded entity.
/// System also manages the player's return to the lobby.
/// </summary>
public sealed partial class MindExtensionSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    private readonly Dictionary<NetUserId, MindExtensionData> _mindExtensions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeRespawnSystemEvents();
        SubscribeTrailSystemEvents();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _mindExtensions.Clear();
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

        return mobState.CurrentState switch
        {
            MobState.Invalid => false,
            MobState.Dead => false,
            _ => true,
        };
    }

    /// <summary>
    /// Retrieves existing extension data for a player or creates a new entry if none exists.
    /// </summary>
    [PublicAPI]
    public MindExtensionData GetOrCreateExtension(NetUserId userId)
    {
        if (_mindExtensions.TryGetValue(userId, out var data))
            return data;

        data = new MindExtensionData();
        _mindExtensions[userId] = data;
        return data;
    }

    /// <summary>
    /// Attempts to find the extension data for a specific user.
    /// </summary>
    [PublicAPI]
    public bool TryGetExtension(NetUserId userId, [NotNullWhen(true)] out MindExtensionData? data)
    {
        return _mindExtensions.TryGetValue(userId, out data);
    }
}
