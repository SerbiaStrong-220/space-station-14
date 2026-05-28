using Content.Shared.Maps;
using Content.Shared.SS220.QuadHearing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.QuadHearing;

public sealed class QuadHearingSystem : SharedQuadHearingSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MapSystem _map = default!;

    public override void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, EntityCoordinates coords, float? range, ICommonSession? predictedSession = null)
    {
        coords = ToMapOrGridCoordinated(coords);
        QuadHearingRegisterTargetMessage? msg = null;
        foreach (var session in _player.Sessions)
        {
            if (session == predictedSession)
                continue;

            if (session.Status is not SessionStatus.InGame)
                continue;

            if (session.AttachedEntity is not { } player)
                continue;

            if (!TryComp<QuadHearingComponent>(player, out var quadHearing))
                continue;

            if (!CanRegisterTarget((player, quadHearing), coords, range))
                continue;

            msg ??= new QuadHearingRegisterTargetMessage
            {
                ProtoId = protoId,
                Coordinates = GetNetCoordinates(coords)
            };

            RaiseNetworkEvent(msg, session);
        }
    }
}
