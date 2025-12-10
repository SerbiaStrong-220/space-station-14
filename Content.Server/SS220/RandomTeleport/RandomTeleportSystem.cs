// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.SS220.InteractionTeleport;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.SS220.RandomTeleport;

public sealed class RandomTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
    }

    private void OnTeleportTarget(Entity<RandomTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        Warp(ent, args.Target, args.User);

        var ev = new TargetTeleportedEvent(args.Target);
        RaiseLocalEvent(ent, ref ev);
    }

    private void Warp(Entity<RandomTeleportComponent> ent, EntityUid teleported, EntityUid user)
    {
        if (ent.Comp.TargetsComponent is null)
            return;

        if (!_componentFactory.TryGetRegistration(ent.Comp.TargetsComponent, out var registration))
            return;

        var validLocations = new List<EntityCoordinates>();

        var query1 = EntityManager.AllEntityQueryEnumerator(registration.Type);
        while (query1.MoveNext(out var target, out _))
        {
            if (_whitelist.IsWhitelistFail(ent.Comp.TeleportTargetWhitelist, target))
                continue;

            validLocations.Add(Transform(target).Coordinates);
        }

        var teleportLocation = _random.Pick(validLocations);

        if (TryComp(user, out PullerComponent? puller) && TryComp(puller.Pulling, out PullableComponent? pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable);

        var xform = Transform(teleported);
        _transformSystem.SetCoordinates(teleported, xform, teleportLocation);

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used linked telepoter {ToPrettyString(ent):teleport} and tried teleport {ToPrettyString(teleported):target} to random location");
    }
}
