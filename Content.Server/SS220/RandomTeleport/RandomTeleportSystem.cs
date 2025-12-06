// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.SS220.InteractionTeleport;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.SS220.RandomTeleport;

public sealed class RandomTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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
            validLocations.Add(Transform(target).Coordinates);
        }

        var teleportLocation = _random.Pick(validLocations);

        //ToDo_SS220 figure out pulling canceling
        //if (TryComp(ent, out PullerComponent? puller) && TryComp(puller.Pulling, out PullableComponent? pullable))
        //    _pulling.TryStopPull(puller.Pulling.Value, pullable);

        var xform = Transform(teleported);
        _transformSystem.SetCoordinates(teleported, xform, teleportLocation);

        //_adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used linked telepoter {ToPrettyString(ent):teleport enter} and tried teleport {ToPrettyString(target):target} to {ToPrettyString(ent.Comp.LinkedEntity.Value):teleport exit}");
    }
}
