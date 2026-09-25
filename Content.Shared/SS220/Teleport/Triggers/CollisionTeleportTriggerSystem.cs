// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.Teleport.Triggers;

public sealed partial class CollisionTeleportTriggerSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollisionTeleportTriggerComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<CollisionTeleportTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.TriggerFixtureId is { } fixtureId && args.OurFixtureId != fixtureId)
            return;

        if (!args.OtherFixture.Hard)
            return;

        var target = args.OtherEntity;
        if (_whitelist.IsWhitelistPass(ent.Comp.DeleteTargetWhitelist, target))
        {
            PredictedQueueDel(target);
            return;
        }

        var attemptEvent = new TeleportUseAttemptEvent(target, target);
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        var request = new TeleportRequestEvent(target, target);
        RaiseLocalEvent(ent, ref request);

        if (request.Handled)
            return;

        if (_net.IsClient)
            return;

        Log.Error($"CollisionTeleportTrigger on {ToPrettyString(ent)} couldn't teleport {ToPrettyString(target)} because no teleport implementation handled the request");
    }
}
