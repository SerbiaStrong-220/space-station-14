// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Client.SS220.Photography;

/// <summary>
/// Client-only lifetime state of the image layer on a physical photograph.
/// </summary>
[RegisterComponent]
public sealed partial class PhotoThumbnailComponent : Component
{
    public PhotoEyeRequest? Request;
    public string AppliedId = string.Empty;
    public TimeSpan RetryAt;
}
