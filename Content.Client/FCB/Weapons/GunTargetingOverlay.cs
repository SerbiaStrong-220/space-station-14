// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Client.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.FCB.Weapons;

public sealed class GunTargetingOverlay : Overlay //Basically WizDen's code with four lines cut off
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

        if (!_guns.TryGetGun(player.Value, out var gunUid, out var gun))
            return;

        var mouseScreenPos = _input.MouseScreenPosition;
        var mousePos = _eye.PixelToMap(mouseScreenPos);

        if (mapPos.MapId != mousePos.MapId)
            return;

        // (☞ﾟヮﾟ)☞
        var maxSpread = gun.MaxAngleModified;
        var minSpread = gun.MinAngleModified;
        var timeSinceLastFire = (_timing.CurTime - gun.NextFire).TotalSeconds;
        var currentAngle = new Angle(MathHelper.Clamp(gun.CurrentAngle.Theta - gun.AngleDecayModified.Theta * timeSinceLastFire,
            gun.MinAngleModified.Theta, gun.MaxAngleModified.Theta));
        var direction = (mousePos.Position - mapPos.Position);

        // Show current angle
        worldHandle.DrawLine(mapPos.Position, mapPos.Position + currentAngle.RotateVec(direction), Color.LightBlue);
        worldHandle.DrawLine(mapPos.Position, mapPos.Position + (-currentAngle).RotateVec(direction), Color.LightBlue);
    }
}
