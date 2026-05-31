using Content.Shared.SS220.QuadHearing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
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

        _overlay = new();
        _overlayManager.AddOverlay(_overlay);

        SubscribeNetworkEvent<QuadHearingRegisterTargetMessage>(OnRegisterTargetMessage);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_overlay != null)
        {
            _overlayManager.RemoveOverlay(_overlay);
            _overlay.Dispose();
            _overlay = null;
        }
    }

    private void OnRegisterTargetMessage(QuadHearingRegisterTargetMessage msg)
    {
        RegisterTarget(msg.ProtoId, GetCoordinates(msg.Coordinates));
    }

    /// <inheritdoc/>
    public override void RegisterTarget(ProtoId<QuadHearingTargetPrototype> protoId, EntityCoordinates coords, ICommonSession? predictedSession = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (predictedSession != null && _player.LocalSession != predictedSession)
            return;

        if (_player.LocalEntity is not { } player)
            return;

        if (!TryComp<QuadHearingComponent>(player, out var quadHearing))
            return;

        coords = ToMapOrGridCoordinates(coords);
        var mapCoords = _transform.ToMapCoordinates(coords);
        var proto = _prototype.Index(protoId);

        var range = GetHearingRange(proto, (player, quadHearing));
        if (!CanRegisterTarget((player, quadHearing), mapCoords, range))
            return;

        var delta = _transform.GetMapCoordinates(player).Position - mapCoords.Position;
        var offset = GetRandomOffset(delta.Length() * proto.RandomOffsetCoefficient);
        coords = new EntityCoordinates(coords.EntityId, coords.Position + offset);

        _overlay?.RegisterTarget(proto, coords);
    }

    private Vector2 GetRandomOffset(float maxDistance)
    {
        var dist = _random.NextFloat(0, maxDistance);
        return _random.NextAngle().ToVec() * dist;
    }
}
