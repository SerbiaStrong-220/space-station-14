// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Projectiles;
using Content.Shared.SS220.Forcefield.Components;
using Content.Shared.SS220.Shuttles.UI;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using static Content.Server.Shuttles.Systems.RadarConsoleSystem;
using static Content.Server.Shuttles.Systems.ShuttleConsoleSystem;

namespace Content.Server.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private HashSet<ICommonSession> _receivers = new();
    private HashSet<EntityUid> _pvsOverrides = new();

    private bool _lastProjectilesEmpty = false;
    private bool _lastForcefieldsEmpty = false;


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

        UpdateProjectiles();
        UpdateForcefields();
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
            if(comp.ShuttleNavProjectileInfo is not { } info ||
                !info.Enabled)
                continue;

            entities.Add(uid);
        }

        var forcefieldsQuery = EntityQueryEnumerator<ForcefieldComponent>();
        while (forcefieldsQuery.MoveNext(out var uid, out var comp))
    }

    private void UpdateProjectiles()
    {
        if (_receivers.Count <= 0)
            return;

        var list = new List<(MapCoordinates, ShuttleNavProjectileInfo)>();
        var query = EntityQueryEnumerator<ProjectileComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ShuttleNavProjectileInfo is not { } info ||
                !info.Enabled)
                continue;

            list.Add((_transform.GetMapCoordinates(uid), info));
        }

        if (list.Count <= 0)
        {
            if (_lastProjectilesEmpty)
                return;
            else
                _lastProjectilesEmpty = true;
        }
        else
            _lastProjectilesEmpty = false;

        var ev = new ShuttleNavInfoUpdateProjectilesMessage(list);
        SendMessageAsync(ev, _receivers);
    }

    private void UpdateForcefields()
    {
        if (_receivers.Count <= 0)
            return;

        var timer = new Stopwatch();
        timer.Start();
        var infos = new List<ShuttleNavForcefieldInfo>();
        var query = EntityQueryEnumerator<ForcefieldComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var localVerts = comp.Params.Shape.CahcedTrianglesVerts;
            var localToWorld = _transform.GetWorldMatrix(uid);
            var mapId = _transform.GetMapId(uid);

            var worldVerts = localVerts.Select(x => new MapCoordinates(Vector2.Transform(x, localToWorld), mapId)).ToList();
            infos.Add(new ShuttleNavForcefieldInfo
            {
                Color = comp.Params.Color,
                TrianglesVerts = worldVerts,
            });
        }
        Log.Info($"Время обновления информации: {timer.Elapsed.TotalMilliseconds} мс");

        if (infos.Count <= 0)
        {
            if (_lastForcefieldsEmpty)
                return;
            else
                _lastForcefieldsEmpty = true;
        }
        else
            _lastForcefieldsEmpty = false;

        timer.Restart();
        var ev = new ShuttleNavInfoUpdateForcefieldsMessage(infos);
        SendMessageAsync(ev, _receivers);
        Log.Info($"Время отправки информации: {timer.Elapsed.TotalMilliseconds} мс");
        Log.Info($"");
    }

    private void SendMessageAsync(EntityEventArgs message, IEnumerable<ICommonSession> sessions)
    {
        for (var i = 0; i < 60; i++)
        {
            foreach (var session in sessions)
                SendMessageToSession(message, session);
        }

        void SendMessageToSession(EntityEventArgs message, ICommonSession session)
        {
            RaiseNetworkEvent(message, session);
        }
    }
}
