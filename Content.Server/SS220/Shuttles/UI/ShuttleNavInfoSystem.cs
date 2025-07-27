// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.SS220.Shuttles.UI;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private static readonly Enum[] ReceiverUiKeys =
    [
        ShuttleConsoleUiKey.Key,
        RadarConsoleUiKey.Key
    ];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateProjectiles();
    }

    public override void AddHitscan(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, ShuttleNavHitscanInfo info)
    {
        if (!info.Enabled)
            return;

        var receivers = GetReceivers();
        if (receivers.Length <= 0)
            return;

        foreach (var receiver in receivers)
        {
            var ev = new ShuttleNavInfoAddHitscanMessage(fromCoordinates, toCoordinates, info);
            RaiseNetworkEvent(ev, receiver);
        }
    }

    private void UpdateProjectiles()
    {
        var receivers = GetReceivers();
        if (receivers.Length <= 0)
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

        foreach (var receiver in receivers)
        {
            var ev = new ShuttleNavInfoUpdateProjectilesMessage(list);
            RaiseNetworkEvent(ev, receiver);
        }
    }

    private ICommonSession[] GetReceivers()
    {
        var sessions = new HashSet<ICommonSession>();
        var query = EntityQueryEnumerator<UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            foreach (var key in ReceiverUiKeys)
                foreach (var actor in _userInterface.GetActors((uid, comp), key))
                    if (_player.TryGetSessionByEntity(actor, out var session))
                        sessions.Add(session);
        }

        return [.. sessions];
    }
}
