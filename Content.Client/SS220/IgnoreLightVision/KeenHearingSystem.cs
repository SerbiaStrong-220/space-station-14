// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.Overlays;
using Content.Shared.SS220.IgnoreLightVision;
using Robust.Client.Graphics;

namespace Content.Client.SS220.IgnoreLightVision;

public sealed class KeenHearingSystem : AddIgnoreLightVisionOverlaySystem<KeenHearingComponent>
{
    protected override Type GetOverlayType()
    {
        return typeof(KeenHearingOverlay);
    }
    protected override Overlay GetOverlayFromConstructor(float radius, float closeRadius)
    {
        return new KeenHearingOverlay(radius, closeRadius);
    }
}
