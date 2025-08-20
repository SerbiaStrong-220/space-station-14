// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Projectiles;
using Content.Shared.SS220.Forcefield.Components;
using Content.Shared.SS220.Shuttles.UI;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Linq;
using static Content.Server.Shuttles.Systems.RadarConsoleSystem;
using static Content.Server.Shuttles.Systems.ShuttleConsoleSystem;

namespace Content.Server.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private readonly HashSet<ICommonSession> _receivers = [];
    private readonly Dictionary<ICommonSession, HashSet<EntityUid>> _pvsOverrides = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadarBoundUIOpenedEvent>(args => OnUIOpened(args.OpenedEvent));
        SubscribeLocalEvent<ShuttleConsoleBoundUIOpenedEvent>(args => OnUIOpened(args.OpenedEvent));

        SubscribeLocalEvent<RadarBoundUIClosedEvent>(args => OnUIClosed(args.ClosedEvent));
        SubscribeLocalEvent<ShuttleConsoleBoundUIClosedEvent>(args => OnUIClosed(args.ClosedEvent));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdatePvsOverrides();
    }

    private void OnUIOpened(BoundUIOpenedEvent args)
    {
        if (_player.TryGetSessionByEntity(args.Actor, out var session))
            _receivers.Add(session);
    }

    private void OnUIClosed(BoundUIClosedEvent args)
    {
        if (_player.TryGetSessionByEntity(args.Actor, out var session))
            _receivers.Remove(session);
    }

    public override void AddHitscan(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, ShuttleNavHitscanInfo info)
    {
        if (!info.Enabled)
            return;

        if (_receivers.Count <= 0)
            return;

        foreach (var receiver in _receivers)
        {
            var ev = new ShuttleNavInfoAddHitscanMessage(fromCoordinates, toCoordinates, info);
            RaiseNetworkEvent(ev, receiver);
        }
    }

    private void UpdatePvsOverrides()
    {
        var entities = new HashSet<EntityUid>();
        var projectilesQuery = EntityQueryEnumerator<ProjectileComponent>();
        while (projectilesQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ShuttleNavProjectileInfo is not { } info ||
                !info.Enabled)
                continue;

            entities.Add(uid);
        }

        var forcefieldsQuery = EntityQueryEnumerator<ForcefieldComponent>();
        while (forcefieldsQuery.MoveNext(out var uid, out _))
            entities.Add(uid);

        var toRemove = _pvsOverrides.Keys.ToHashSet();
        foreach (var session in _receivers)
        {
            var overrides = _pvsOverrides.GetOrNew(session);
            foreach (var ent in entities)
                _pvsOverride.AddSessionOverride(ent, session);

            _pvsOverrides.TryAdd(session, overrides);
            toRemove.Remove(session);
        }

        foreach (var session in toRemove)
        {
            if (!_pvsOverrides.TryGetValue(session, out var overrides))
                continue;

            foreach (var ent in overrides)
                _pvsOverride.RemoveSessionOverride(ent, session);

            _pvsOverrides.Remove(session);
        }
    }
}
