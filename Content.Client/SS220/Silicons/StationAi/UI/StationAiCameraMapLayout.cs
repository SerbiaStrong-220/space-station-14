// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Client.SS220.Silicons.StationAi.UI;

public readonly record struct StationAiCameraMapLayout(bool Portrait, float MapHeight, float SidebarWidth)
{
    /// <summary>
    /// Side-by-side when wide; map above the camera list when narrow or tall.
    /// Unused dimensions are NaN so they can be assigned to SetWidth/SetHeight directly.
    /// The threshold sits above the landscape minimum (map header ~540 + sidebar),
    /// so shrinking a landscape window always reaches the portrait layout.
    /// </summary>
    public static StationAiCameraMapLayout Calculate(float width, float height)
    {
        return width < 900 || height > width
            ? new StationAiCameraMapLayout(true, Math.Clamp(height * 0.45f, 200, 560), float.NaN)
            : new StationAiCameraMapLayout(false, float.NaN, Math.Clamp(width * 0.3f, 240, 420));
    }
}
