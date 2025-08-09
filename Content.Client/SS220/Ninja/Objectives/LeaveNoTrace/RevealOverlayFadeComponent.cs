// Original code licensed under Imperial CLA.
// Copyright holders: orix0689 (discord) and pocchitsu (discord)
// See Content.Shared/SS220/Ninja/Objectives/LeaveNoTrace/license.txt

// Modified and/or redistributed under SS220 CLA with hosting restrictions.
// Full license text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client.SS220.Ninja.Objectives.LeaveNoTrace;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RevealOverlayFadeComponent : Component
{
    /// <summary>
    /// A time after we remove <see cref="RevealOverlay" />
    /// </summary>
    [DataField]
    public TimeSpan RemoveRevealOverlayTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The exact time we remove the component
    /// </summary>
    [DataField(readOnly: true, customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RemoveRevealOverlayEndTime = TimeSpan.Zero;
}
