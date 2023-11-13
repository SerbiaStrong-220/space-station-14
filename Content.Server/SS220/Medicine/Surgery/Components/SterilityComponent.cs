// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Medicine.Surgery.Components;

[RegisterComponent]
public sealed partial class SterilityComponent : Component
{
    /// <summary>
    /// Used for surfaces (like tables or surgical tables), floors, instruments, clothes and etc
    /// 1 - Good
    /// 0 - 0.9 - Not good, we have a chance to infect a limb, organ, y o u r s o u l =<
    /// </summary>

    [DataField("sterility")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Sterility = 1.0f;
}

