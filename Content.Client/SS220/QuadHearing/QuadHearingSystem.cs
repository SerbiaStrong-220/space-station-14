using Content.Shared.SS220.QuadHearing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Client.SS220.QuadHearing;

public sealed class QuadHearingSystem : SharedQuadHearingSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private QuadHearingOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<QuadHearingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<QuadHearingComponent, ComponentRemove>(OnRemove);
        SubscribeNetworkEvent<QuadHearingRegisterTargetMessage>(OnRegisterTargetMessage);
    }

    private void OnInit(Entity<QuadHearingComponent> ent, ref ComponentInit args)
    {
        if (_overlay != null)
            return;

        _overlay = new();
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnRemove(Entity<QuadHearingComponent> ent, ref ComponentRemove args)
    {
        if (_overlay == null)
            return;

        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Dispose();
        _overlay = null;
    }

    private void OnRegisterTargetMessage(QuadHearingRegisterTargetMessage msg)
    {
        RegisterTarget(msg.ProtoId, GetCoordinates(msg.Coordinates), null);
    }

    public override void RegisterTarget(ProtoId<QuadHearingTargetTypePrototype> protoId, EntityCoordinates coords, float? range, ICommonSession? predictedSession = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (predictedSession != null && _player.LocalSession != predictedSession)
            return;

        if (_player.LocalEntity is not { } player)
            return;

        if (!TryComp<QuadHearingComponent>(player, out var quadHearing))
            return;

        coords = ToMapOrGridCoordinated(coords);
        if (!CanRegisterTarget((player, quadHearing), coords, range))
            return;

        var proto = _prototype.Index(protoId);
        var delta = _transform.GetMapCoordinates(player).Position - _transform.ToMapCoordinates(coords).Position;
        var offset = GetRandomOffset(delta.Length() * proto.RandomOffsetCoefficient);
        coords = new EntityCoordinates(coords.EntityId, coords.Position + offset);

        _overlay?.RegisterTarget(proto, coords);
    }

    private Vector2 GetRandomOffset(float maxDistance)
    {
        var dist = _random.NextFloat(0, maxDistance);
        var offset = new Vector2(dist, 0);

        var angle = _random.NextAngle();
        var rotMatrix = Matrix3x2.CreateRotation((float)angle.Theta);

        return Vector2.Transform(offset, rotMatrix);
    }
}
