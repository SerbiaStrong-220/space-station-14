using Content.Server.Actions;
using Content.Shared.SS220.QuadHearing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.QuadHearing;

public sealed class QuadHearingSystem : SharedQuadHearingSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    private static readonly EntProtoId ActionId = "ActionToggleQuadHearing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<QuadHearingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<QuadHearingComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<QuadHearingComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ToggleActionEntity, ActionId);
        Dirty(ent);
    }

    private void OnRemove(Entity<QuadHearingComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.ToggleActionEntity);
        Dirty(ent);
    }

    /// <inheritdoc/>
    public override void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, EntityCoordinates coords, float? range, ICommonSession? predictedSession = null)
    {
        coords = ToMapOrGridCoordinates(coords);
        var mapCoords = _transform.ToMapCoordinates(coords);
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

            if (!CanRegisterTarget((player, quadHearing), mapCoords, range))
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
