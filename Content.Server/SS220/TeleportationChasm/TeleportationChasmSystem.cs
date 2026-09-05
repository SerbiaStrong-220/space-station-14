// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.TeleportationChasm;
using Robust.Shared.Timing;

namespace Content.Server.SS220.TeleportationChasm;

public sealed partial class TeleportationChasmSystem : SharedTeleportationChasmSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeleportationChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasmFalling))
        {
            if (_timing.CurTime < chasmFalling.FallEndTime)
                continue;

            if (chasmFalling.DeleteInsteadOfTeleport)
            {
                QueueDel(uid);
                continue;
            }

            if (chasmFalling.SourceTeleporter is not { } teleporter)
            {
                Log.Error($"TeleportationChasm couldn't teleport {ToPrettyString(uid)} because the teleporter was not specified");
                QueueDel(uid);
                continue;
            }

            if (TerminatingOrDeleted(teleporter))
            {
                Log.Error($"TeleportationChasm couldn't teleport {ToPrettyString(uid)} because {ToPrettyString(teleporter)} was terminating or deleted");
                QueueDel(uid);
                continue;
            }

            var request = new TeleportRequestEvent(uid, uid);
            RaiseLocalEvent(teleporter, ref request);

            if (!request.Handled)
            {
                Log.Error($"TeleportationChasm couldn't teleport {ToPrettyString(uid)} because no teleport implementation handled the request");
                QueueDel(uid);
                continue;
            }

            RemCompDeferred<TeleportationChasmFallingComponent>(uid);
            _blocker.UpdateCanMove(uid);
        }
    }
}
