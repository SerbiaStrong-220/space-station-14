// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.SS220.Weapons.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Weapons;

public sealed class GunTargetingOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private IEntityManager _entManager;
    private readonly IEyeManager _eye;
    private readonly IGameTiming _timing;
    private readonly IInputManager _input;
    private readonly IPlayerManager _player;
    private readonly GunSystem _guns;
    private readonly SharedTransformSystem _transform;

    public GunTargetingOverlay(IEntityManager entManager, IEyeManager eyeManager, IGameTiming timing, IInputManager input, IPlayerManager player, GunSystem system, SharedTransformSystem transform)
    {
        _entManager = entManager;
        _eye = eyeManager;
        _input = input;
        _timing = timing;
        _player = player;
        _guns = system;
        _transform = transform;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;

        var player = _player.LocalEntity;

        if (player == null ||
            !_entManager.TryGetComponent<TransformComponent>(player, out var xform))
        {
            return;
        }

        var mapPos = _transform.GetMapCoordinates(player.Value, xform: xform);

        if (mapPos.MapId == MapId.Nullspace)
            return;

        if (!_guns.TryGetGun(player.Value, out var gun))
            return;

        var mouseScreenPos = _input.MouseScreenPosition;
        var mousePos = _eye.PixelToMap(mouseScreenPos);

        if (mapPos.MapId != mousePos.MapId)
            return;

        // (☞ﾟヮﾟ)☞
        var maxSpread = gun.Comp.MaxAngleModified;
        var minSpread = gun.Comp.MinAngleModified;
        var timeSinceLastFire = (_timing.CurTime - gun.Comp.NextFire).TotalSeconds;
        var currentAngle = new Angle(MathHelper.Clamp(gun.Comp.CurrentAngle.Theta - gun.Comp.AngleDecayModified.Theta * timeSinceLastFire,
            gun.Comp.MinAngleModified.Theta, gun.Comp.MaxAngleModified.Theta));
        var direction = (mousePos.Position - mapPos.Position);

        var overlayColor = Color.LightBlue;

        if (_entManager.TryGetComponent<GunAimableComponent>(gun.Owner, out var aimableComp) && aimableComp.IsAimed)
            overlayColor = Color.Orange;

        // Show current angle
        worldHandle.DrawCircle(mapPos.Position + currentAngle.RotateVec(direction), 0.08f, overlayColor, true);
        worldHandle.DrawCircle(mapPos.Position + (-currentAngle).RotateVec(direction), 0.08f, overlayColor, true);
    }
}
