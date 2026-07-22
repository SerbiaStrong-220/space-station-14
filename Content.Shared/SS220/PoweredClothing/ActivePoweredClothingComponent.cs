// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

namespace Content.Shared.SS220.PoweredClothing;

[RegisterComponent]
public sealed partial class ActivePoweredClothingComponent : Component
{
    [DataField]
    public TimeSpan TargetTime = TimeSpan.Zero;
}
