// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Silicons.StationAi.CameraMap.UI;

/// <summary>
///     Nav map that fills its container and names the marker under the cursor.
/// </summary>
public sealed class StationAiCameraNavMapControl : NavMapControl
{
    private const float HoverTooltipDelay = 0.1f;

    /// <summary>
    ///     Tooltip text for each tracked marker.
    /// </summary>
    public readonly Dictionary<NetEntity, string> Names = new();

    public StationAiCameraNavMapControl()
    {
        WallColor = new Color(192, 122, 196);
        TileColor = new Color(71, 42, 72);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));
        TooltipDelay = HoverTooltipDelay;
        TrackingTooltip = true;

        // The base control is a fixed square with a fixed-width header; let both follow the window.
        SetSize = new Vector2(float.NaN, float.NaN);
        foreach (var container in Children)
        {
            foreach (var child in container.Children)
            {
                if (child is PanelContainer)
                    child.SetWidth = float.NaN;
            }
        }
    }

    protected override Vector2 MidPointVector => PixelSize / 2f;

    protected override int MidPoint => Math.Max(1, Math.Min(PixelWidth, PixelHeight) / 2);

    protected override float MinimapScale => WorldRange != 0
        ? Math.Max(1f, MidPoint - MinimapMargin * UIScale) / WorldRange
        : 0f;

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        var name = NameAt(args.RelativePixelPosition);
        if (ToolTip == name)
            return;

        HideTooltip();
        ToolTip = name;
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        HideTooltip();
        ToolTip = null;
    }

    private string? NameAt(Vector2 mouse)
    {
        string? found = null;
        var closest = MaxSelectableDistance;
        foreach (var (uid, blip) in TrackedEntities)
        {
            var local = blip.Coordinates.Position - GetOffset();
            var distance = Vector2.Distance(mouse, ScalePosition(new Vector2(local.X, -local.Y)));
            if (distance > closest || !Names.TryGetValue(uid, out var name))
                continue;

            closest = distance;
            found = name;
        }

        return found;
    }
}
