// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Silicons.StationAi.UI;

public sealed class StationAiCameraMapControl : NavMapControl
{
    public readonly Dictionary<NetEntity, string> Names = new();
    private Vector2? _mousePosition;

    public StationAiCameraMapControl()
    {
        WallColor = new Color(192, 122, 196);
        TileColor = new Color(71, 42, 72);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));
        TooltipDelay = 0.1f;
        TrackingTooltip = true;
        // Fill the window instead of the base 648px square; the header panel follows the width.
        SetSize = new Vector2(float.NaN, float.NaN);
        MinSize = new Vector2(240, 240);
        foreach (var container in Children)
        {
            foreach (var child in container.Children)
            {
                if (child is PanelContainer)
                    child.SetWidth = float.NaN;
            }
        }
    }

    // Center on the actual control and fit the shorter side, so any window size shows the whole range.
    protected override Vector2 MidPointVector => PixelSize / 2f;

    protected override int MidPoint => Math.Max(1, Math.Min(PixelWidth, PixelHeight) / 2);

    protected override float MinimapScale => WorldRange != 0
        ? Math.Max(1f, MidPoint - MinimapMargin * UIScale) / WorldRange
        : 0f;

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        _mousePosition = args.RelativePixelPosition;
        UpdateTooltip();
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _mousePosition = null;
        HideTooltip();
        ToolTip = null;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        // Re-evaluate even with a stationary cursor when a camera disappears or the map zooms.
        UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        if (_mousePosition is not { } mouse)
            return;

        string? hoveredName = null;
        var closest = MaxSelectableDistance;
        foreach (var (uid, blip) in TrackedEntities)
        {
            var local = blip.Coordinates.Position - GetOffset();
            var screen = ScalePosition(new Vector2(local.X, -local.Y));
            var distance = Vector2.Distance(mouse, screen);
            if (distance > closest || !Names.TryGetValue(uid, out var name))
                continue;

            closest = distance;
            hoveredName = name;
        }

        if (ToolTip == hoveredName)
            return;

        HideTooltip();
        ToolTip = hoveredName;
    }
}
