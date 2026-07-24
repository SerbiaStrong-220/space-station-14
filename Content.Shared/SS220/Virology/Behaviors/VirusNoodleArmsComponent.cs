// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusNoodleArmsComponent : Component
{
    /// <summary>If true, anything held or picked up drops immediately.</summary>
    [DataField]
    public bool AlwaysDrop;

    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(45);

    [ViewVariables]
    public TimeSpan? NextSpasm;
}
