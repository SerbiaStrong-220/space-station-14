// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.SS220.Photography;

/// <summary>
/// Performs the single pending offscreen readback inside the render loop, not in FrameUpdate.
/// </summary>
public sealed class PhotoThumbnailOverlay(PhotoVisualizer visualizer) : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpaceBelowWorld;

    protected override void Draw(in OverlayDrawArgs args)
    {
        visualizer.TryRenderThumbnail();
    }
}
