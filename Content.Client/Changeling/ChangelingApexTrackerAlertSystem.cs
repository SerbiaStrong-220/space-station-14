// SS220 changeling Apex tracker
using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Alert;
using Content.Shared.Changeling.Mutations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Changeling;

/// <summary>
/// Rotates the owner-only Apex Predator HUD arrow towards the server-selected target.
/// </summary>
public sealed class ChangelingApexTrackerAlertSystem : EntitySystem
{
    private static readonly ProtoId<AlertPrototype> TrackingAlert = "ChangelingApexTarget";

    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingApexTrackerComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    private void OnUpdateAlertSprite(
        Entity<ChangelingApexTrackerComponent> ent,
        ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != TrackingAlert)
            return;

        var angle = ent.Comp.ArrowAngle + _eyeManager.CurrentEye.Rotation;
        _sprite.LayerSetRotation(
            args.SpriteViewEnt.AsNullable(),
            AlertVisualLayers.Base,
            angle);
    }
}
